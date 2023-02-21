using Microsoft.EntityFrameworkCore;
using VRAtlas.Core;
using VRAtlas.Core.Models;
using VRAtlas.Events;
using VRAtlas.Models;
using VRAtlas.Services;

namespace VRAtlas.Listeners;

public class EventAnnouncementListener : IScopedEventListener<EventStatusUpdatedEvent>
{
    private readonly AtlasContext _atlasContext;
    private readonly IEventService _eventService;
    private readonly INotificationService _notificationService;

    public EventAnnouncementListener(AtlasContext atlasContext, IEventService eventService, INotificationService notificationService)
    {
        _atlasContext = atlasContext;
        _eventService = eventService;
        _notificationService = notificationService;
    }

    public async Task Handle(EventStatusUpdatedEvent message)
    {
        // Only do stuff if this was an event announcement.
        if (message.Status is not EventStatus.Announced)
            return;

        // <Notice>
        // This is the most complex notification delivery setup currently in the project.
        // We need to ensure that we don't send out multiple notifications at once for the same event.
        // For these notifications, we need to notify those who follow the Group and Event Stars who have already been confirmed.

        // Get the event from the subject
        var atlasEvent = await _eventService.GetEventByIdAsync(message.Id);

        if (atlasEvent is null)
            throw new Exception($"Unable to find event with id {message.Id}. This should not happen.");

        // <Part 1>
        //  First, we want to notify all the users who follow any of the event stars starring in this event.

        //  Select the user ids of the stars that are CONFIRMED starring in this event.
        var starsConfirmedIds = atlasEvent.Stars.Where(s => s.Status is EventStarStatus.Confirmed).Select(s => s.User!.Id);

        //  Fetch the user ids of those who follow the stars.
        var starAssociatedUserIds = await _atlasContext.Follows
            .Where(f => f.EntityType == EntityType.User && starsConfirmedIds.Contains(f.EntityId))
            .Select(f => f.UserId)
            .Distinct() // Must be unique
            .ToArrayAsync();

        //  Dispatch the notification for these users.
        if (starAssociatedUserIds.Length > 0)
        {
            // Construct the "names" text (PERSON A, PERSON B, and PERSON C)
            string names = atlasEvent.Stars.Count switch
            {
                1 => FormatEventStar(atlasEvent.Stars[0]), // We don't want an "and" when there's only one user.
                2 => $"{FormatEventStar(atlasEvent.Stars[0])} and {FormatEventStar(atlasEvent.Stars[1])}", // We don't want a "," when there's only two users.
                >= 3 => atlasEvent.Stars.SkipLast(1).Select(FormatEventStar).Aggregate((a, b) => $"{a}, {b}") + $", and {FormatEventStar(atlasEvent.Stars.Last())}",
                _ => string.Empty,
            };

            string title = $"Event Announced - {atlasEvent.Name} by {atlasEvent.Owner!.Name}";
            string description = $"See {names} star at {atlasEvent.Name} hosted by {atlasEvent.Owner!.Name}.";

            await _notificationService.CreateNotificationAsync(atlasEvent.Id, EntityType.Event, NotificationKeys.EventAnnouncement, title, description, starAssociatedUserIds);
        }

        // <Part 2>
        //  Now, we want to notify everyone who doesn't follow the stars but *does* follow the group.

        //  Fetch the user ids of those who follow the group.
        var groupAssociatedUserIds = await _atlasContext.Follows
            .Where(f => f.EntityType == EntityType.Group && f.EntityId == atlasEvent.Owner!.Id)
            .Select(f => f.UserId)
            .Where(uid => !starAssociatedUserIds.Contains(uid)) // Filter out anyone who just received one of the star notifications.
            .Distinct() // Must be unique
            .ToArrayAsync();

        if (groupAssociatedUserIds.Length > 0)
        {
            string title = $"Event Announced - {atlasEvent.Name} by {atlasEvent.Owner!.Name}";
            string description = $"{atlasEvent.Owner!.Name} is hosting {atlasEvent.Name}.";

            await _notificationService.CreateNotificationAsync(atlasEvent.Id, EntityType.Event, NotificationKeys.EventAnnouncement, title, description, groupAssociatedUserIds);
        }
    }

    private static string FormatEventStar(EventStar star)
    {
        if (string.IsNullOrWhiteSpace(star.Title))
            return star.User!.Username;

        return $"{star.User!.Username} ({star.Title})";
    }
}