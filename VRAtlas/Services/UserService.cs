using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using VRAtlas.Models;

namespace VRAtlas.Services;

public interface IUserService
{
    Task<User?> GetUserAsync(Guid id);
    Task<User?> GetUserAsync(ClaimsPrincipal principal);
    Task<IEnumerable<User>> GetUsersAsync(string search);
}

public class UserService : IUserService
{
    private readonly AtlasContext _atlasContext;

    public UserService(AtlasContext atlasContext)
    {
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
        return await _atlasContext.Users.Where(u => u.Username.ToLower().Contains(search.ToLower())).Take(pageSize).ToArrayAsync();
    }
}