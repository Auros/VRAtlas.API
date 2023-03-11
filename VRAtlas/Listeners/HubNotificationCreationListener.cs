using Microsoft.AspNetCore.SignalR;
using VRAtlas.Core.Models;
using VRAtlas.Events;

namespace VRAtlas.Listeners;

public class HubNotificationCreationListener : IScopedEventListener<NotificationCreatedEvent>
{
    private readonly IHubContext<AtlasHub> _atlasHubContext;

    public HubNotificationCreationListener(IHubContext<AtlasHub> atlasHubContext)
    {
        _atlasHubContext = atlasHubContext;
    }

    public Task Handle(NotificationCreatedEvent message)
    {
        var (notif, user) = message;

        var proxy = _atlasHubContext.Clients.User(user.SocialId);
        return proxy.SendAsync("notificationReceived", new
        {
            id = notif.Id,
            key = notif.Key,
            title = notif.Title,
            description = notif.Description,
            entityId = notif.EntityId,
            entityType = notif.EntityType,
            createdAt = notif.CreatedAt.ToString(),
            read = notif.Read
        });
    }
}