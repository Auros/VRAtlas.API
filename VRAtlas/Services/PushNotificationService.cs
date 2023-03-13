using Lib.Net.Http.WebPush;
using Microsoft.EntityFrameworkCore;
using NodaTime;
using VRAtlas.Logging;
using VRAtlas.Models;

namespace VRAtlas.Services;

public interface IPushNotificationService
{
    /// <summary>
    /// Adds a web push subscription for a user.
    /// </summary>
    /// <param name="userId">The id of the user.</param>
    /// <param name="endpoint">The endpoint of the subscription.</param>
    /// <param name="p256dh">The p256dh crypto key of the subscription.</param>
    /// <param name="auth">The auth key of the subscription.</param>
    /// <returns></returns>
    Task<PushSubscription> SubscribeAsync(Guid userId, string endpoint, string p256dh, string auth);

    /// <summary>
    /// Remove a web push notification.
    /// </summary>
    /// <param name="endpoint">The endpoint.</param>
    /// <returns>If the endpoint existed and could be successfully removed or not.</returns>
    Task<bool> UnsubscribeAsync(string endpoint);

    /// <summary>
    /// Gets a user's subscriptions.
    /// </summary>
    /// <param name="userId">The id of the user.</param>
    /// <returns></returns>
    Task<IEnumerable<PushSubscription>> GetUserSubscriptionsAsync(Guid userId);
}

public class PushNotificationService : IPushNotificationService
{
    private readonly IClock _clock;
    private readonly IAtlasLogger _atlasLogger;
    private readonly AtlasContext _atlasContext;

    public PushNotificationService(IClock clock, IAtlasLogger<PushNotificationService> atlasLogger, AtlasContext atlasContext)
    {
        _clock = clock;
        _atlasLogger = atlasLogger;
        _atlasContext = atlasContext;
    }

    public async Task<IEnumerable<PushSubscription>> GetUserSubscriptionsAsync(Guid userId)
    {
        var subs = await _atlasContext.WebPushSubscriptions.Where(s => s.UserId == userId).ToArrayAsync();
        return subs.Select(s =>
        {
            PushSubscription sub = new();
            sub.SetKey(PushEncryptionKeyName.Auth, s.Auth);
            sub.SetKey(PushEncryptionKeyName.P256DH, s.P256DH);
            sub.Endpoint = s.Endpoint;
            return sub;
        });
    }

    public async Task<PushSubscription> SubscribeAsync(Guid userId, string endpoint, string p256dh, string auth)
    {
        _atlasContext.WebPushSubscriptions.Add(new WebPushSubscription
        {
            UserId = userId,
            Auth = auth,
            P256DH = p256dh,
            CreatedAt = _clock.GetCurrentInstant(),
            Endpoint = endpoint
        });

        await _atlasContext.SaveChangesAsync();

        PushSubscription sub = new();
        sub.SetKey(PushEncryptionKeyName.Auth, endpoint);
        sub.SetKey(PushEncryptionKeyName.P256DH, endpoint);
        sub.Endpoint = endpoint;

        _atlasLogger.LogInformation("User {UserId} successfully created a push notification hook", userId);

        return sub;
    }

    public async Task<bool> UnsubscribeAsync(string endpoint)
    {
        var sub = await _atlasContext.WebPushSubscriptions.FirstOrDefaultAsync(s => s.Endpoint == endpoint);
        if (sub is null)
            return false;

        _atlasContext.WebPushSubscriptions.Remove(sub);
        await _atlasContext.SaveChangesAsync();
        _atlasLogger.LogInformation("Push notification hook removed");
        return true;
    }
}