using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using VRAtlas.Logging;
using VRAtlas.Models;

namespace VRAtlas.Services;

public interface IUserService
{
    Task<User?> GetUserAsync(Guid id);
    Task<User?> GetUserAsync(ClaimsPrincipal principal);
    Task<IEnumerable<User>> GetUsersAsync(string search);
    Task<User?> EditUserAsync(ClaimsPrincipal principal, string bio, IEnumerable<string> links, ProfileStatus profileStatus, NotificationMetadata notificationMetadata);
}

public class UserService : IUserService
{
    private readonly IAtlasLogger _atlasLogger;
    private readonly AtlasContext _atlasContext;

    public UserService(IAtlasLogger<UserService> atlasLogger, AtlasContext atlasContext)
    {
        _atlasLogger = atlasLogger;
        _atlasContext = atlasContext;
    }

    public Task<User?> GetUserAsync(Guid id)
    {
        return _atlasContext.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id);
    }

    public Task<User?> GetUserAsync(ClaimsPrincipal principal)
    {
        var socialId = principal.FindFirstValue(ClaimTypes.NameIdentifier);

        if (socialId is null)
            return Task.FromResult<User?>(null);

        return _atlasContext.Users.AsNoTracking().FirstOrDefaultAsync(u => u.SocialId == socialId);
    }

    public async Task<IEnumerable<User>> GetUsersAsync(string search)
    {
        const int pageSize = 10;

        // If the search is an id, pull the user from that.
        if (Guid.TryParse(search, out var id))
        {
            return await _atlasContext.Users.Where(u => u.Id == id).Take(1).ToArrayAsync();
        }

        // Search for users by their username.
        return await _atlasContext.Users.Where(u => u.Username.ToLower().Contains(search.ToLower())).OrderBy(u => u.Id).Take(pageSize).ToArrayAsync();
    }

    public async Task<User?> EditUserAsync(ClaimsPrincipal principal, string bio, IEnumerable<string> links, ProfileStatus profileStatus, NotificationMetadata notificationMetadata)
    {
        var socialId = principal.FindFirstValue(ClaimTypes.NameIdentifier);

        if (socialId is null)
            return null;

        var user = await _atlasContext.Users.Include(u => u.DefaultNotificationSettings).FirstOrDefaultAsync(u => u.SocialId == socialId);
        if (user is null)
            return null;
        
        user.Biography = bio;
        user.Links = links.ToList();
        user.ProfileStatus = profileStatus;
        user.DefaultNotificationSettings!.AtStart = notificationMetadata.AtStart;
        user.DefaultNotificationSettings!.AtThirtyMinutes = notificationMetadata.AtThirtyMinutes;
        user.DefaultNotificationSettings!.AtOneHour = notificationMetadata.AtOneHour;
        user.DefaultNotificationSettings!.AtOneDay = notificationMetadata.AtOneDay;

        await _atlasContext.SaveChangesAsync();
        _atlasLogger.LogInformation("User {UserId} updated their profile", user.Id);

        return user;
    }
}