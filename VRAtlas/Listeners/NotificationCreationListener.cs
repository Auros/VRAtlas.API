using Microsoft.AspNetCore.SignalR;
using VRAtlas.Core.Models;
using VRAtlas.Events;

namespace VRAtlas.Listeners;

public class NotificationCreationListener : IScopedEventListener<NotificationCreatedEvent>
{
    private readonly IHubContext<AtlasHub> _atlasHubContext;

    public NotificationCreationListener(IHubContext<AtlasHub> atlasHubContext)
    {
        _atlasHubContext = atlasHubContext;
    }

    public Task Handle(NotificationCreatedEvent message)
    {
        var (notif, user) = message;

        var proxy = _atlasHubContext.Clients.User(user.Id.ToString());
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