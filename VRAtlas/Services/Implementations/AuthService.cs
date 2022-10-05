using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.Json;
using VRAtlas.Models;

namespace VRAtlas.Services.Implementations;

public class AuthService : IAuthService
{
    private const string _discordCdnUrl = "https://cdn.discordapp.com";

    private readonly AtlasContext _atlasContext;
    private readonly IImageCdnService _imageCdnService;

    public AuthService(AtlasContext atlasContext, IImageCdnService imageCdnService)
    {
        _atlasContext = atlasContext;
        _imageCdnService = imageCdnService;
    }

    public async Task<User?> GetUserAsync(ClaimsPrincipal principal)
    {
        // Check claims to ensure that there is an Id here.
        var id = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (id is null)
            return null;

        // Then we find which platform it belongs to.
        var discordHash = principal.FindFirstValue("urn:discord:avatar:hash");
        if (discordHash is null)
            return null; // In the future we'll move this to check to a separate method to check multiple platforms more seemlessly, but for now we only have one platform...

        string? pfpUrl = null;
        var user = await _atlasContext.Users.Include(u => u.Roles).FirstOrDefaultAsync(u => u.Identifiers.DiscordId == id);
        if (user is null)
        {
            // If the user is not in the database, we create them!
            user = new User
            {
                Id = Guid.NewGuid(),
                Name = principal.FindFirstValue(ClaimTypes.Name) ?? id,
                Identifiers = new PlatformIdentifiers { DiscordId = id }
            };

            // Add the user to the context
            _atlasContext.Users.Add(user);
        }
        pfpUrl = $"{_discordCdnUrl}/avatars/{id}/{discordHash}.{(discordHash.StartsWith("a_") ? "gif" : "png")}"; // If the discord hash starts with a_, it's animated, so we should request the animated format.
        if (pfpUrl != null && pfpUrl != user.IconSourceUrl)
        {
            // Upload their profile picture to our image CDN service.
            // We upload it to our own platform instead of using the URL from the platform because sometimes those URL expire when that user changes their profile picture
            // We *could* run a service which automatically checks for those, but that's more of a hassle than just uploading all the variants ourself.
            var variants = await _imageCdnService.UploadAsync(pfpUrl, JsonSerializer.Serialize(new { Source = nameof(VRAtlas), Context = "User", Identifier = id }));
            if (variants is null)
                return null; // For simplicity throughout the entire stack, we will require profile pictures.

            user.IconSourceUrl = pfpUrl;
            user.Icon = variants;
        }

        user.Name = principal.FindFirstValue(ClaimTypes.Name) ?? id;
        user.Email = principal.FindFirstValue(ClaimTypes.Email);

        // If the user has no roles, add the default one.
        // The default role should be guaranteed to exist from the setup process.
        if (user.Roles.Count == 0)
        {
            var defaultRole = await _atlasContext.Roles.FirstAsync(r => r.Name == AtlasConstants.DefaultRoleName);
            user.Roles.Add(defaultRole);   
        }

        // Save any changes we might have made
        await _atlasContext.SaveChangesAsync();
        return user;
    }
}