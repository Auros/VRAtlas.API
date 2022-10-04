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

        var user = await _atlasContext.Users.FirstOrDefaultAsync(u => u.Identifiers.DiscordId == id);
        return user;
    }
}