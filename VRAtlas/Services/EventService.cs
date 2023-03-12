using MessagePipe;
using Microsoft.EntityFrameworkCore;
using NodaTime;
using VRAtlas.Events;
using VRAtlas.Logging;
using VRAtlas.Models;
using static VRAtlas.Services.IEventService;

namespace VRAtlas.Services;

public interface IEventService
{
    /// <summary>
    /// Checks if an event exists.
    /// </summary>
    /// <param name="id">The id of the event.</param>
    /// <returns>Whether or not the event exists.</returns>
    Task<bool> EventExistsAsync(Guid id);

    /// <summary>
    /// Gets an event with a specific id if it exists.
    /// </summary>
    /// <param name="id">The id of the event.</param>
    /// <returns>The event, or null if it does not exist.</returns>
    Task<Event?> GetEventByIdAsync(Guid id);

    public record struct EventCollectionQueryOptions(Guid? Cursor, Guid? Group, EventStatus? Status, int PageSize = 25);
    public record struct EventCollectionQueryResult(IEnumerable<Event> Events, Guid? NextCursor);
    Task<EventCollectionQueryResult> QueryEventsAsync(EventCollectionQueryOptions options);

    /// <summary>
    /// Creates an event.
    /// </summary>
    /// <param name="name">The name of the event</param>
    /// <param name="ownerId">The owning group of the id.</param>
    /// <param name="mediaId">The id of the media element.</param>
    /// <returns>The new event.</returns>
    Task<Event> CreateEventAsync(string name, Guid ownerId, Guid mediaId);

    /// <summary>
    /// Updates an event's info.
    /// </summary>
    /// <param name="id">The id of the event.</param>
    /// <param name="name">The name of the event</param>
    /// <param name="description">The description of the event.</param>
    /// <param name="media">The media id of the event.</param>
    /// <param name="tags">The tags of the event.</param>
    /// <param name="eventStars">The stars in this event.</param>
    /// <param name="updater">The person updating the group.</param>
    /// <returns>The updated event or null if it doesn't exist.</returns>
    Task<Event?> UpdateEventAsync(Guid id, string name, string description, Guid? media, IEnumerable<string> tags, IEnumerable<EventStarInfo> eventStars, Guid updater, bool autoStart);

    /// <summary>
    /// Announces an event.
    /// </summary>
    /// <param name="id">The id of the event.</param>
    /// <returns></returns>
    Task AnnounceEventAsync(Guid id);

    /// <summary>
    /// Starts an event.
    /// </summary>
    /// <param name="id">The id of the event.</param>
    /// <returns></returns>
    Task StartEventAsync(Guid id);

    /// <summary>
    /// Concludes an event.
    /// </summary>
    /// <param name="id">The id of the event.</param>
    /// <returns></returns>
    Task ConcludeEventAsync(Guid id);

    /// <summary>
    /// Cancels an event.
    /// </summary>
    /// <param name="id">The id of the event.</param>
    /// <returns></returns>
    Task CancelEventAsync(Guid id);

    /// <summary>
    /// Schedules an event.
    /// </summary>
    /// <param name="id">The id of the event.</param>
    /// <param name="startTime">The start time of the event.</param>
    /// <param name="endTime">The end time of the event.</param>
    /// <returns>The updated event, if it exists.</returns>
    Task<Event?> ScheduleEventAsync(Guid id, Instant startTime, Instant? endTime);

    /// <summary>
    /// Gets the id of the owning group associated with an event.
    /// </summary>
    /// <param name="id">The id of the event.</param>
    /// <returns>The id of the group, if the event exists. Is default if the event does not exist.</returns>
    Task<Guid> GetEventGroupIdAsync(Guid id);

    /// <summary>
    /// Checks to see if an event can be scheduled.
    /// </summary>
    /// <param name="id">The id of the event.</param>
    /// <returns>Can it be scheduled?</returns>
    Task<bool> CanScheduleEventAsync(Guid id);

    /// <summary>
    /// Accepts a star invitation.
    /// </summary>
    /// <param name="id">The id of the event.</param>
    /// <param name="userId">The id of the user to accept.</param>
    /// <returns>Was the acceptance successful? This is false when there was no pending invite.</returns>
    Task<bool> AcceptStarInvitationAsync(Guid id, Guid userId);

    /// <summary>
    /// Rejects a star invitation.
    /// </summary>
    /// <param name="id">The id of the event.</param>
    /// <param name="userId">The id of the user to reject.</param>
    /// <returns>Was the rejection successful? This is false when there was no pending invite.</returns>
    Task<bool> RejectStarInvitationAsync(Guid id, Guid userId);

    /// <summary>
    /// Gets the status of an event.
    /// </summary>
    /// <param name="id">The id of the event.</param>
    /// <returns>The status of the event, or null if the event doesn't exist.</returns>
    Task<EventStatus?> GetEventStatusAsync(Guid id);
}

public class EventService : IEventService
{
    private readonly ITagService _tagService;
    private readonly IAtlasLogger _atlasLogger;
    private readonly AtlasContext _atlasContext;
    private readonly IPublisher<EventCreatedEvent> _eventCreated;
    private readonly IPublisher<EventScheduledEvent> _eventScheduled;
    private readonly IPublisher<EventStatusUpdatedEvent> _eventStatusUpdated;
    private readonly IPublisher<EventStarInvitedEvent> _eventStarInvited;
    private readonly IPublisher<EventStarAcceptedInviteEvent> _eventStarAccepted;

    public EventService(
        ITagService tagService,
        IAtlasLogger<EventService> atlasLogger,
        AtlasContext atlasContext,
        IPublisher<EventCreatedEvent> eventCreated,
        IPublisher<EventScheduledEvent> eventScheduled,
        IPublisher<EventStatusUpdatedEvent> eventStatusUpdated,
        IPublisher<EventStarInvitedEvent> eventStarInvited,
        IPublisher<EventStarAcceptedInviteEvent> eventStarAccepted)
    {
        _tagService = tagService;
        _atlasLogger = atlasLogger;
        _atlasContext = atlasContext;
        _eventCreated = eventCreated;
        _eventScheduled = eventScheduled;
        _eventStatusUpdated = eventStatusUpdated;
        _eventStarInvited = eventStarInvited;
        _eventStarAccepted = eventStarAccepted;
    }

    public Task<Event?> GetEventByIdAsync(Guid id)
    {
        return _atlasContext.Events
            .AsNoTracking()
            .Include(e => e.Stars)
                .ThenInclude(s => s.User)
            .Include(e => e.Owner)
                .ThenInclude(g => g!.Members)
                    .ThenInclude(m => m.User)
            .Include(e => e.Tags)
                .ThenInclude(e => e.Tag)
            .Include(e => e.RSVP)
            .AsSplitQuery()
            .FirstOrDefaultAsync(e => e.Id == id);
    }

    public async Task<EventCollectionQueryResult> QueryEventsAsync(EventCollectionQueryOptions options)
    {
        var (cursor, group, status, pageSize) = options;
        pageSize = 0 > pageSize ? 25 : pageSize; // Ensure page size is greater than zero
        IQueryable<Event> query = _atlasContext.Events.AsNoTracking().Include(e => e.Tags).ThenInclude(t => t.Tag);
        query = query.OrderByDescending(e => e.StartTime);
        
        if (cursor is not null)
        {
            // Gets the StartTime value of the cursor element.
            var targetTime = await _atlasContext.Events.AsNoTracking().Where(e => e.Id == cursor.Value).Select(e => e.StartTime).FirstOrDefaultAsync();

            // Paginate via cursor
            query = query.Where(e => targetTime >= e.StartTime);
            cursor = null;
        }

        // If we need a specifc group, filter by group.
        if (group is not null)
        {
            query = query.Where(q => q.Owner!.Id == group.Value);
        }
        if (status is not null)
        {
            query = query.Where(q => q.Status == status.Value);
        }

        query = query.Take(pageSize + 1); // Add one to calculate the next cursor element

        var events = await query.ToListAsync(); // Load the elements

        if (events.Count == pageSize + 1) // If we have an extra element than expected, that means we need to record what the next cursor is.
        {
            var lastEvent = events[^1];
            events.Remove(lastEvent);

            // If we're not at the very last element in the entire query search (oldest event), set the next page cursor indicating that there's more elements.
            if (events.Count is not 0)
                cursor = lastEvent.Id;
        }

        return new EventCollectionQueryResult(query, cursor);
    }

    public async Task<Event> CreateEventAsync(string name, Guid ownerId, Guid mediaId)
    {
        _atlasLogger.LogDebug("Creating a new event named {EventName} by {OwnerId}", name, ownerId);
        var group = await _atlasContext.Groups.Include(g => g.Members).ThenInclude(m => m.User).FirstOrDefaultAsync(g => g.Id == ownerId) ?? throw new InvalidOperationException($"Group {ownerId} does not exist.");

        Event atlasEvent = new()
        {
            Id = Guid.NewGuid(),
            Name = name,
            Description = string.Empty,
            Owner = group,
            Stars = new List<EventStar>(),
            Media = mediaId,
            Status = EventStatus.Unlisted
        };

        _atlasContext.Events.Add(atlasEvent);
        await _atlasContext.SaveChangesAsync();
        _atlasLogger.LogInformation("Successfully created a new event with id {EventId} and name {EventName}", atlasEvent.Id, atlasEvent.Name);
        _eventCreated.Publish(new EventCreatedEvent(group.Id));
        return atlasEvent;
    }

    public Task<bool> EventExistsAsync(Guid id)
    {
        return _atlasContext.Events.AnyAsync(e => e.Id == id);
    }

    public async Task<Event?> UpdateEventAsync(Guid id, string name, string description, Guid? media, IEnumerable<string> tags, IEnumerable<EventStarInfo> eventStars, Guid updater, bool autoStart)
    {
        _atlasLogger.LogDebug("Updating the event {EventId}", id);

        // Pre-create any tags up here.
        // TODO: Reuse the captured ids.
        foreach (var tag in tags)
            await _tagService.CreateTagAsync(tag, updater);

        var atlasEvent = await _atlasContext.Events
            .Include(e => e.Stars)
                .ThenInclude(s => s.User)
            .Include(s => s.Owner)
            .AsSplitQuery()
            .FirstOrDefaultAsync(e => e.Id == id);

        if (atlasEvent is null)
            return null;

        atlasEvent.Name = name;
        if (media.HasValue)
            atlasEvent.Media = media.Value;
        atlasEvent.Description = description;
        atlasEvent.AutoStart = autoStart;

        // Load and remove any previous tags.
        var eventTags = await _atlasContext.EventTags.Where(t => t.Event.Id == id).ToArrayAsync();
        if (eventTags.Any())
            _atlasContext.EventTags.RemoveRange(eventTags);

        // Search for the new tags and add them to the DB
        var tagSearch = tags.Select(t => t.ToLower());
        var relevantTags = await _atlasContext.Tags.Where(t => tagSearch.Contains(t.Name.ToLower())).ToArrayAsync();
        _atlasContext.EventTags.AddRange(relevantTags.Select(t => new EventTag
        {
            Event = atlasEvent,
            Tag = t
        }));
        await _atlasContext.SaveChangesAsync();

        List<EventStar> addedStars = new();
        if (eventStars.Any())
        {
            var newStarsIds = eventStars.ToArray();
            var removedStars = atlasEvent.Stars.Where(s => !newStarsIds.Any(n => n.Star == s.User!.Id));
            var addedStarsIds = newStarsIds.Where(s => !atlasEvent.Stars.Any(es => es.User!.Id == s.Star));
            var existingStars = newStarsIds.Where(s => !addedStarsIds.Contains(s));

            // Remove any stars that were removed
            atlasEvent.Stars.RemoveAll(removedStars.Contains);
            foreach (var star in addedStarsIds)
            {
                // Look for the user.
                var user = await _atlasContext.Users.FirstOrDefaultAsync(u => u.Id == star.Star);

                // If they don't exist, skip over them.
                if (user is null)
                    continue;

                // If you are inviting yourself, you don't have to go through the invitation process.
                var status = updater != star.Star ? EventStarStatus.Pending : EventStarStatus.Confirmed;

                // Add them to the event.
                EventStar eventStar = new()
                {
                    Title = string.IsNullOrWhiteSpace(star.Title) ? null : star.Title,
                    Status = status,
                    User = user
                };
                atlasEvent.Stars.Add(eventStar);
                addedStars.Add(eventStar);
            }

            foreach (var star in existingStars)
            {
                atlasEvent.Stars.First().Title = string.IsNullOrWhiteSpace(star.Title) ? null : star.Title;
            }

            await _atlasContext.SaveChangesAsync();
        }
        else
        {
            atlasEvent.Stars.Clear();
            await _atlasContext.SaveChangesAsync();
        }

        // Send notifications for recently added event stars.
        foreach (var star in addedStars)
        {
            if (star.Status is EventStarStatus.Confirmed)
                _eventStarAccepted.Publish(new EventStarAcceptedInviteEvent(atlasEvent!.Id, star.User!.Id));
            else if (star.Status is EventStarStatus.Pending)
                _eventStarInvited.Publish(new EventStarInvitedEvent(atlasEvent!.Id, star.User!.Id));
        }

        // Return a fresh event for consumers (since we updated the object indirectly)
        atlasEvent = await GetEventByIdAsync(id);
        _atlasLogger.LogInformation("Successfully updated the event {EventId}", id);

        return atlasEvent;
    }

    public async Task AnnounceEventAsync(Guid id)
    {
        var atlasEvent = await _atlasContext.Events.FirstOrDefaultAsync(e => e.Id == id);
        if (atlasEvent is null)
        {
            _atlasLogger.LogWarning("Could not find event when trying to announce {EventId}", id);
            return;
        }

        if (atlasEvent.Status is not EventStatus.Unlisted)
        {
            _atlasLogger.LogInformation("Tried to announce an event that was not previously unlisted with id {EventId}", id);
            return;
        }

        atlasEvent.Status = EventStatus.Announced;
        await _atlasContext.SaveChangesAsync();

        _eventStatusUpdated.Publish(new EventStatusUpdatedEvent(atlasEvent.Id, atlasEvent.Status));
    }

    public async Task StartEventAsync(Guid id)
    {
        var atlasEvent = await _atlasContext.Events.FirstOrDefaultAsync(e => e.Id == id);
        if (atlasEvent is null)
        {
            _atlasLogger.LogWarning("Could not find event when trying to start {EventId}", id);
            return;
        }
        
        if (atlasEvent.Status is not EventStatus.Announced)
        {
            _atlasLogger.LogInformation("Tried to start an event that was not previously announced with id {EventId}", id);
            return;
        }

        atlasEvent.Status = EventStatus.Started;
        await _atlasContext.SaveChangesAsync();

        _atlasLogger.LogInformation("Successfully started the event {EventId}", id);
        _eventStatusUpdated.Publish(new EventStatusUpdatedEvent(atlasEvent.Id, atlasEvent.Status));
    }

    public async Task ConcludeEventAsync(Guid id)
    {
        var atlasEvent = await _atlasContext.Events.FirstOrDefaultAsync(e => e.Id == id);
        if (atlasEvent is null)
        {
            _atlasLogger.LogWarning("Could not find event when trying to conclude {EventId}", id);
            return;
        }

        if (atlasEvent.Status is not EventStatus.Started or EventStatus.Announced)
        {
            _atlasLogger.LogWarning("Tried to conclude an event that was not previously started or announced with id {EventId}", id);
            return;
        }    

        atlasEvent.Status = EventStatus.Concluded;
        await _atlasContext.SaveChangesAsync();

        _atlasLogger.LogInformation("Successfully concluded the event {EventId}", id);
        _eventStatusUpdated.Publish(new EventStatusUpdatedEvent(atlasEvent.Id, atlasEvent.Status));
    }

    public async Task CancelEventAsync(Guid id)
    {
        var atlasEvent = await _atlasContext.Events.FirstOrDefaultAsync(e => e.Id == id);
        if (atlasEvent is null)
        {
            _atlasLogger.LogWarning("Could not find event when trying to cancel {EventId}", id);
            return;
        }

        if (atlasEvent.Status is not EventStatus.Started or EventStatus.Announced)
        {
            _atlasLogger.LogWarning("Tried to cancel an event that was not previously started or announced with id {EventId}", id);
            return;
        }

        atlasEvent.Status = EventStatus.Canceled;
        await _atlasContext.SaveChangesAsync();

        _atlasLogger.LogInformation("Successfully canceled the event {EventId}", id);
        _eventStatusUpdated.Publish(new EventStatusUpdatedEvent(atlasEvent.Id, atlasEvent.Status));
    }

    public async Task<Event?> ScheduleEventAsync(Guid id, Instant startTime, Instant? endTime)
    {
        var atlasEvent = await _atlasContext.Events
            .Include(e => e.Tags)
                .ThenInclude(e => e.Tag)
            .Include(e => e.Owner)
                .ThenInclude(g => g!.Members)
                    .ThenInclude(m => m.User)
            .Include(e => e.Stars)
                .ThenInclude(es => es.User)
            .AsSplitQuery()
            .FirstOrDefaultAsync(e => e.Id == id);

        if (atlasEvent is null)
        {
            _atlasLogger.LogWarning("Could not find event when trying to schedule {EventId}", id);
            return null;
        }    

        if (atlasEvent.Status is not EventStatus.Unlisted && atlasEvent.Status is not EventStatus.Announced)
        {
            _atlasLogger.LogWarning("Tried to schedule an event that's not unlisted or announced with id {EventId}", id);
            return atlasEvent;
        }    

        var oldTime = atlasEvent.StartTime;
        var oldEndTime = atlasEvent.EndTime;

        atlasEvent.EndTime = endTime;
        atlasEvent.StartTime = startTime;

        await _atlasContext.SaveChangesAsync();
        _atlasLogger.LogInformation("Successfully scheduled event {EventId}", id);

        if ((oldTime.HasValue || oldEndTime.HasValue) && (startTime != oldTime || endTime != oldEndTime))
        {
            _atlasLogger.LogInformation("Publishing new schedule event for event {EventId}", id);
            _eventScheduled.Publish(new EventScheduledEvent(id));
        }

        return atlasEvent;
    }

    public Task<Guid> GetEventGroupIdAsync(Guid id)
    {
        return _atlasContext.Events.Where(e => e.Id == id).Select(e => e.Owner!.Id).FirstOrDefaultAsync();
    }

    public Task<bool> CanScheduleEventAsync(Guid id)
    {
        return _atlasContext.Events.AnyAsync(e => e.Id == id && (e.Status == EventStatus.Unlisted || e.Status == EventStatus.Announced));
    }

    public async Task<bool> AcceptStarInvitationAsync(Guid id, Guid userId)
    {
        _atlasLogger.LogInformation("User {StarUserId} is trying to accept the invite for event {EventId}", userId, id);
        var atlasEvent = await _atlasContext.Events.Include(e => e.Stars).ThenInclude(s => s.User).FirstOrDefaultAsync(e => e.Id == id);
        if (atlasEvent is null)
        {
            _atlasLogger.LogWarning("Could not find event {EventId} when trying to accept the invite for {StarUserId}", id, userId);
            return false;
        }    

        var star = atlasEvent.Stars.FirstOrDefault(s => s.User!.Id == userId);
        if (star is null || star.Status is not EventStarStatus.Pending)
        {
            _atlasLogger.LogWarning("Could not find pending invite for {StarUserId} in event {EventId}", userId, id);
            return false;
        }    

        star.Status = EventStarStatus.Confirmed;
        await _atlasContext.SaveChangesAsync();

        _atlasLogger.LogInformation("User {StarUserId} successfully accepted invite for the event {EventId}", userId, id);
        _eventStarAccepted.Publish(new EventStarAcceptedInviteEvent(atlasEvent!.Id, star.User!.Id));

        return true;
    }

    public async Task<bool> RejectStarInvitationAsync(Guid id, Guid userId)
    {
        _atlasLogger.LogInformation("User {StarUserId} is trying to reject the invite for event {EventId}", userId, id);
        var atlasEvent = await _atlasContext.Events.Include(e => e.Stars).ThenInclude(s => s.User).FirstOrDefaultAsync(e => e.Id == id);
        if (atlasEvent is null)
        {
            _atlasLogger.LogWarning("Could not find event {EventId} when trying to reject the invite for {StarUserId}", id, userId);
            return false;
        }    

        var star = atlasEvent.Stars.FirstOrDefault(s => s.User!.Id == userId);
        if (star is null || star.Status is not EventStarStatus.Pending)
        {
            _atlasLogger.LogWarning("Could not find pending invite for {StarUserId} in event {EventId}", userId, id);
            return false;
        }    

        star.Status = EventStarStatus.Rejected;
        await _atlasContext.SaveChangesAsync();

        _atlasLogger.LogInformation("User {StarUserId} successfully rejected invite for the event {EventId}", userId, id);
        return true;
    }

    public async Task<EventStatus?> GetEventStatusAsync(Guid id)
    {
        // You cannot get nullable primitives from a FirstOrDefault EF call, so we wrap the status in a Select
        var data = await _atlasContext.Events.Where(e => e.Id == id).Select(e => new { e.Status }).FirstOrDefaultAsync();
        if (data is null)
            return null;
        return data.Status;
    }
}