using VRAtlas.Core.Models;
using VRAtlas.Core;
using VRAtlas.Events;
using VRAtlas.Models;
using VRAtlas.Services;
using Microsoft.EntityFrameworkCore;

namespace VRAtlas.Listeners;

public class EventCancellationListener : IScopedEventListener<EventStatusUpdatedEvent>
{
    private readonly AtlasContext _atlasContext;
    private readonly IEventService _eventService;
    private readonly INotificationService _notificationService;

    public EventCancellationListener(AtlasContext atlasContext, IEventService eventService, INotificationService notificationService)
    {
        _atlasContext = atlasContext;
        _eventService = eventService;
        _notificationService = notificationService;
    }
    public async Task Handle(EventStatusUpdatedEvent message)
    {
        // Only do stuff if this was an event cancellation event.
        if (message.Status is not EventStatus.Canceled)
            return;

        // Get the event from the subject
        var atlasEvent = await _eventService.GetEventByIdAsync(message.Id);

        if (atlasEvent is null)
            throw new Exception($"Unable to find event with id {message.Id}. This should not happen.");

        // Fetch the user ids of those who follow this event.
        var subscribedUserIds = await _atlasContext.Follows
            .Where(f => f.EntityType == EntityType.Event && f.EntityId == atlasEvent.Id)
            .Select(f => f.UserId)
            .ToArrayAsync();

        if (subscribedUserIds.Length <= 0)
            return;

        string title = $"Event Cancelled - {atlasEvent.Name}";
        string description = $"The event {atlasEvent.Name} was cancelled by {atlasEvent.Owner!.Name}.";
        await _notificationService.CreateNotificationAsync(atlasEvent.Id, EntityType.Event, NotificationKeys.EventCancelled, title, description, subscribedUserIds);
    }
}