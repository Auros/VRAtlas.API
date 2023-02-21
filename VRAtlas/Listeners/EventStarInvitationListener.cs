using VRAtlas.Core;
using VRAtlas.Core.Models;
using VRAtlas.Events;
using VRAtlas.Models;
using VRAtlas.Services;

namespace VRAtlas.Listeners;

public class EventStarInvitationListener : IScopedEventListener<EventStarInvitedEvent>
{
    private readonly IEventService _eventService;
    private readonly INotificationService _notificationService;

    public EventStarInvitationListener(IEventService eventService, INotificationService notificationService)
    {
        _eventService = eventService;
        _notificationService = notificationService;
    }  

    public async Task Handle(EventStarInvitedEvent message)
    {
        var (eventId, starId) = message;

        // Get the event from the subject
        var atlasEvent = await _eventService.GetEventByIdAsync(eventId);

        if (atlasEvent is null)
            throw new Exception($"Unable to find event with id {eventId}. This should not happen.");

        // The star SHOULD ALWAYS BE in this list if this method is firing.
        var star = atlasEvent.Stars.First(s => s.User!.Id == starId);

        string title = $"Invitation - Star at {atlasEvent.Name}";
        string description = $"You've been invited to star at {atlasEvent.Name} (hosted by {atlasEvent.Owner!.Name}).";
        await _notificationService.CreateNotificationAsync(atlasEvent.Id, EntityType.Event, NotificationKeys.EventStarInvited, title, description, starId);
    }
}