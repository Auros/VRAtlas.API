using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace VRAtlas.Tests.Setup;

/// <summary>
/// Immediately returns a user if there's a match.
/// </summary>
internal class TestAuthService : IAuthService
{
    private readonly AtlasContext _atlasContext;

    public TestAuthService(AtlasContext atlasContext)
    {
        _atlasContext = atlasContext;
    }

    public async Task<User?> GetUserAsync(ClaimsPrincipal principal)
    {
        // Check claims to ensure that there is an Id here.
        var id = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (id is null)
            return null;

        var user = await _atlasContext.Users.Include(u => u.Roles).FirstOrDefaultAsync(u => u.Identifiers.DiscordId == id);
        
        // If there are no roles, assign the default
        if (user != null && !user.Roles.Any())
        {
            user.Roles.Add(await _atlasContext.Roles.FirstAsync(r => r.Name == AtlasConstants.DefaultRoleName));
            await _atlasContext.SaveChangesAsync();
        }

        return user;
    }
}