using Lib.Net.Http.WebPush;
using System.Text.Json;
using VRAtlas.Core.Models;
using VRAtlas.Events;
using VRAtlas.Services;

namespace VRAtlas.Listeners;

public class PushNotificationCreationListener : IScopedEventListener<NotificationCreatedEvent>
{
    private readonly PushServiceClient _pushServiceClient;
    private readonly IPushNotificationService _pushNotificationService;

    public PushNotificationCreationListener(PushServiceClient pushServiceClient, IPushNotificationService pushNotificationService)
    {
        _pushServiceClient = pushServiceClient;
        _pushNotificationService = pushNotificationService;
    }

    public async Task Handle(NotificationCreatedEvent message)
    {
        var (notif, user) = message;

        var subscriptions = await _pushNotificationService.GetUserSubscriptionsAsync(user.Id);
        PushMessage pushMessage = new(JsonSerializer.Serialize(new
        {
            id = notif.Id,
            key = notif.Key,
            title = notif.Title,
            description = notif.Description,
            entityId = notif.EntityId,
            entityType = notif.EntityType,
            createdAt = notif.CreatedAt.ToString(),
            read = notif.Read
        }));

        await Parallel.ForEachAsync(subscriptions, async (sub, token) =>
        {
            await _pushServiceClient.RequestPushMessageDeliveryAsync(sub, pushMessage, token);
        });
    }
}