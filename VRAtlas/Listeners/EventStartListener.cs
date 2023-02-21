using Microsoft.EntityFrameworkCore;
using VRAtlas.Core;
using VRAtlas.Core.Models;
using VRAtlas.Events;
using VRAtlas.Models;
using VRAtlas.Services;

namespace VRAtlas.Listeners;

public class EventStartListener : IScopedEventListener<EventStatusUpdatedEvent>
{
    private readonly AtlasContext _atlasContext;
    private readonly IEventService _eventService;
    private readonly INotificationService _notificationService;

    public EventStartListener(AtlasContext atlasContext, IEventService eventService, INotificationService notificationService)
    {
        _atlasContext = atlasContext;
        _eventService = eventService;
        _notificationService = notificationService;
    }
    public async Task Handle(EventStatusUpdatedEvent message)
    {
        // Only do stuff if this was an event start event.
        if (message.Status is not EventStatus.Started)
            return;

        // Get the event from the subject
        var atlasEvent = await _eventService.GetEventByIdAsync(message.Id);

        if (atlasEvent is null)
            throw new Exception($"Unable to find event with id {message.Id}. This should not happen.");

        // Fetch the user ids of those who follow this event.
        var subscribedUserIds = await _atlasContext.Follows
            .Where(f => f.EntityType == EntityType.Event && f.EntityId == atlasEvent.Id && f.Metadata.AtStart)
            .Select(f => f.UserId)
            .ToArrayAsync();

        if (subscribedUserIds.Length <= 0)
            return;

        string title = $"Event Starting Now - {atlasEvent.Name}";
        string description = $"The event {atlasEvent.Name} by {atlasEvent.Owner!.Name} is starting now! Hope to see you there!";
        await _notificationService.CreateNotificationAsync(atlasEvent.Id, EntityType.Event, NotificationKeys.EventStarted, title, description, subscribedUserIds);
    }
}
