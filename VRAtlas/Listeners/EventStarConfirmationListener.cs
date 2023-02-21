using Microsoft.EntityFrameworkCore;
using VRAtlas.Core;
using VRAtlas.Core.Models;
using VRAtlas.Events;
using VRAtlas.Models;
using VRAtlas.Services;

namespace VRAtlas.Listeners;

public class EventStarConfirmationListener : IScopedEventListener<EventStarAcceptedInviteEvent>
{
    private readonly AtlasContext _atlasContext;
    private readonly IEventService _eventService;
    private readonly INotificationService _notificationService;

    public EventStarConfirmationListener(AtlasContext atlasContext, IEventService eventService, INotificationService notificationService)
    {
        _atlasContext = atlasContext;
        _eventService = eventService;
        _notificationService = notificationService;
    }

    public async Task Handle(EventStarAcceptedInviteEvent message)
    {
        var (eventId, starId) = message;

        // Get the event from the subject
        var atlasEvent = await _eventService.GetEventByIdAsync(eventId);

        if (atlasEvent is null)
            throw new Exception($"Unable to find event with id {eventId}. This should not happen.");

        // Only broadcast if the event has already been announced
        if (atlasEvent.Status is EventStatus.Unlisted)
            return;

        // The star SHOULD ALWAYS BE in this list if this method is firing.
        var star = atlasEvent.Stars.First(s => s.User!.Id == starId);

        // Fetch the user ids of those who follow the target user.
        var subscribedUserIds = await _atlasContext.Follows
            .Where(f => f.EntityType == EntityType.User && f.EntityId == starId)
            .Select(f => f.UserId)
            .ToArrayAsync();

        string title = $"{star.User!.Username} is at {atlasEvent.Name}";
        string description = $"{star.User!.Username} is a star at {atlasEvent.Name} hosted by {atlasEvent.Owner!.Name}";
        await _notificationService.CreateNotificationAsync(atlasEvent.Id, EntityType.Event, NotificationKeys.EventStarConfirmed, title, description, subscribedUserIds);


    }
}