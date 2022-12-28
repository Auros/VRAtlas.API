using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using VRAtlas.Models;

namespace VRAtlas.Services;

public interface IUserService
{
    Task<User?> GetUserAsync(Guid id);
    Task<User?> GetUserAsync(ClaimsPrincipal principal);
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
}