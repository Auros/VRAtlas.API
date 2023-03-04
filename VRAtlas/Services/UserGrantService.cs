using LitJWT;
using Microsoft.EntityFrameworkCore;
using NodaTime;
using System.Text.Json.Serialization;
using VRAtlas.Logging;
using VRAtlas.Models;

namespace VRAtlas.Services;

public interface IUserGrantService
{
    Task<User> GrantUserAsync(UserTokens tokens);
}

public class UserGrantService : IUserGrantService
{
    private readonly IClock _clock;
    private readonly JwtDecoder _jwtDecoder;
    private readonly IAtlasLogger _atlasLogger;
    private readonly AtlasContext _atlasContext;
    private readonly IImageCdnService _imageCdnService;

    public UserGrantService(IClock clock, JwtDecoder jwtDecoder, IAtlasLogger<UserGrantService> atlasLogger, AtlasContext atlasContext, IImageCdnService imageCdnService)
    {
        _clock = clock;
        _jwtDecoder = jwtDecoder;
        _atlasLogger = atlasLogger;
        _atlasContext = atlasContext;
        _imageCdnService = imageCdnService;
    }

    public async Task<User> GrantUserAsync(UserTokens tokens)
    {
        // Read the id token
        if (_jwtDecoder.TryDecode<IdentifierPayload>(tokens.IdToken, out var payload) is not DecodeResult.Success)
            throw new InvalidOperationException("Could not validate id token");

        // Look for the user based on their social id
        var user = await _atlasContext.Users.Include(u => u.Metadata).FirstOrDefaultAsync(u => u.SocialId == payload.SocialId);
        bool newUser = false;

        // If the profile picture is from Discord, we increase its size for better resolution before reuploading it.
        var payloadPicture = payload.SocialId.Contains("discord", StringComparison.InvariantCultureIgnoreCase) ? new Uri(payload.Picture.ToString() + "?size=2048") : payload.Picture;

        // Check if the user has not been created yet
        if (user is null)
        {
            newUser = true;
            _atlasLogger.LogInformation("Creating a new user with the username {Username} and social platform identifier {PlatformId}", payload.Name, payload.SocialId);
            // If so, create the user with the required values and add them to the context
            user = new User
            {
                Id = Guid.NewGuid(),
                Username = payload.Name,
                SocialId = payload.SocialId,
                JoinedAt = _clock.GetCurrentInstant(),
                LastLoginAt = _clock.GetCurrentInstant(),
                Metadata = new UserMetadata
                {
                    SynchronizeUsernameWithSocialPlatform = true,
                    SynchronizeProfilePictureWithSocialPlatform = true,
                    CurrentSocialPlatformUsername = payload.Name,
                    CurrentSocialPlatformProfilePicture = payloadPicture.ToString(),
                },
                DefaultNotificationSettings = new NotificationMetadata
                {
                    AtStart = true,
                    AtOneHour = true
                }
            };
            _atlasContext.Users.Add(user);
        }

        // If username synchronization is on and the username from the most recent identity update does not match the current user, update them.
        if (user.Metadata!.SynchronizeUsernameWithSocialPlatform && payload.Name != user.Username)
        {
            _atlasLogger.LogInformation("Reassigning {UserId}'s username from {OldUsername} to {NewUsername}", user.Id, user.Username, payload.Name);
            user.Username = payload.Name;
            user.Metadata.CurrentSocialPlatformUsername = user.Username;
        }

        // If profile picture synchronization is on and the profile picture from the most recent identity update does not match the current user, update them.
        if (newUser || (user.Metadata.SynchronizeProfilePictureWithSocialPlatform && payloadPicture != user.Metadata.ProfilePictureUrl))
        {
            _atlasLogger.LogInformation("Reassigning {UserId}'s profile picture from {OldPictureSource} to {NewPictureSource}", user.Id, user.Metadata.ProfilePictureUrl, payloadPicture);

            // Upload their profile picture to our image CDN service.
            // We upload it to our own platform instead of using the URL from the platform because sometimes those URL expire when that user changes their profile picture
            // We *could* run a service which automatically checks for those, but that's more of a hassle and requires extra state management for those user's identities and state within Auth0.
            var identifier = await _imageCdnService.UploadAsync(payloadPicture, $$"""
                {
                    "source": "{{nameof(VRAtlas)}}",
                    "context": "user",
                    "identifier": "{{user.Id}}"
                }
                """);

            user.Picture = identifier;
            user.Metadata.CurrentSocialPlatformProfilePicture = payloadPicture.ToString();
        }

        user.LastLoginAt = _clock.GetCurrentInstant();
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