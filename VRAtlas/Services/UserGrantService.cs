using LitJWT;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Text.Json.Serialization;
using VRAtlas.Logging;
using VRAtlas.Models;
using VRAtlas.Options;

namespace VRAtlas.Services;

public interface IUserGrantService
{
    Task<User> GrantUserAsync(UserTokens tokens);
}

public class UserGrantService : IUserGrantService
{
    private readonly JwtDecoder _jwtDecoder;
    private readonly IAtlasLogger _atlasLogger;
    private readonly AtlasContext _atlasContext;

    public UserGrantService(JwtDecoder jwtDecoder, IAtlasLogger<UserGrantService> atlasLogger, AtlasContext atlasContext)
    {
        _jwtDecoder = jwtDecoder;
        _atlasLogger = atlasLogger;
        _atlasContext = atlasContext;
    }

    public async Task<User> GrantUserAsync(UserTokens tokens)
    {
        // Read the id token
        if (_jwtDecoder.TryDecode<IdentifierPayload>(tokens.IdToken, out var payload) is not DecodeResult.Success)
            throw new InvalidOperationException("Could not validate id token");

        // Look for the user based on their social id
        var user = await _atlasContext.Users.FirstOrDefaultAsync(u => u.SocialId == payload.SocialId);
        
        // Check if the user has not been created yet
        if (user is null)
        {
            _atlasLogger.LogInformation("Creating a new user with the username {Username} and social platform identifier {PlatformId}", payload.Name, payload.SocialId);
            // If so, create the user with the required values and add them to the context
            user = new User
            {
                Id = Guid.NewGuid(),
                Username = payload.Name,
                SocialId = payload.SocialId,
            };
            _atlasContext.Users.Add(user);
        }

        await _atlasContext.SaveChangesAsync();
        return user;
    }

    private class IdentifierPayload
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = null!;

        [JsonPropertyName("picture")]
        public Uri Picture { get; set; } = null!;

        [JsonPropertyName("sub")]
        public string SocialId { get; set; } = null!;
    }
}