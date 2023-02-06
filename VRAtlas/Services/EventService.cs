using Microsoft.EntityFrameworkCore;
using NodaTime;
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

    public record struct EventCollectionQueryOptions(Guid? Cursor, int PageSize = 25);
    public record struct EventCollectionQueryResult(IEnumerable<Event> Events, Guid? NextCursor, Guid? PreviousCursor);
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
    Task<Event?> UpdateEventAsync(Guid id, string name, string description, Guid media, IEnumerable<string> tags, IEnumerable<Guid> eventStars, Guid updater);

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
}

public class EventService : IEventService
{
    private readonly ITagService _tagService;
    private readonly AtlasContext _atlasContext;

    public EventService(ITagService tagService, AtlasContext atlasContext)
    {
        _tagService = tagService;
        _atlasContext = atlasContext;
    }

    public Task<Event?> GetEventByIdAsync(Guid id)
    {
        return _atlasContext.Events
            .AsNoTracking()
            .Include(e => e.Stars)
                .ThenInclude(s => s.User)
            .Include(e => e.Owner)
            .Include(e => e.Tags)
            .Include(e => e.RSVP)
            .FirstOrDefaultAsync();
    }

    public async Task<EventCollectionQueryResult> QueryEventsAsync(EventCollectionQueryOptions options)
    {
        Guid? previous = null;
        var (cursor, pageSize) = options;
        pageSize = 0 > pageSize ? 25 : pageSize; // Ensure page size is greater than zero
        var query = _atlasContext.Events.AsNoTracking();
        query = query.OrderByDescending(e => e.StartTime);
        
        if (cursor is not null)
        {
            // Gets the StartTime value of the cursor element.
            var targetTime = await _atlasContext.Events.AsNoTracking().Where(e => e.Id == cursor.Value).Select(e => e.StartTime).FirstOrDefaultAsync();

            // Paginate via cursor
            query = query.Where(e => targetTime >= e.StartTime);
            cursor = null;
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

        if (events.Count > 0)
        {
            var firstEvent = events[0];

            // Queries the last page and gets the id for its first element. 
            previous = await _atlasContext.Events.AsNoTracking().OrderBy(e => e.StartTime).Where(e => e.StartTime > firstEvent.StartTime).Take(pageSize).Select(e => e.Id).LastOrDefaultAsync();
        }

        return new EventCollectionQueryResult(query, cursor, previous);
    }

    public async Task<Event> CreateEventAsync(string name, Guid ownerId, Guid mediaId)
    {
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
        return atlasEvent;
    }

    public Task<bool> EventExistsAsync(Guid id)
    {
        return _atlasContext.Events.AnyAsync(e => e.Id == id);
    }

    public async Task<Event?> UpdateEventAsync(Guid id, string name, string description, Guid media, IEnumerable<string> tags, IEnumerable<Guid> eventStars, Guid updater)
    {
        var atlasEvent = await _atlasContext.Events
            .Include(e => e.Tags)
                .ThenInclude(e => e.Tag)
            .Include(e => e.Owner)
            .Include(e => e.Stars)
                .ThenInclude(es => es.User)
            .FirstOrDefaultAsync(e => e.Id == id);

        if (atlasEvent is null)
            return null;

        atlasEvent.Name = name;
        atlasEvent.Media = media;
        atlasEvent.Description = description;

        List<EventTag> addedEventTags = new();
        foreach (var tagName in tags)
        {
            var search = tagName.ToLower();
            var currentEventTag = atlasEvent.Tags.FirstOrDefault(et => et.Tag.Name.ToLower() == search.ToLower());

            if (currentEventTag is not null)
            {
                addedEventTags.Add(currentEventTag);
                continue;
            }    

            var tag = await _tagService.GetTagAsync(tagName);
            tag ??= await _tagService.CreateTagAsync(tagName, updater);

            EventTag eventTag = new()
            {
                Id = Guid.NewGuid(),
                Tag = tag,
                Event = atlasEvent
            };

            addedEventTags.Add(eventTag);
            atlasEvent.Tags.Add(eventTag);
        }

        foreach (var tagToRemove in atlasEvent.Tags.Where(ae => !addedEventTags.Contains(ae)))
            atlasEvent.Tags.Remove(tagToRemove);

        List<EventStar> addedEventStars = new();
        foreach (var starId in eventStars)
        {
            var currentStar = atlasEvent.Stars.FirstOrDefault(es => es.User!.Id == starId);
            if (currentStar is not null)
            {
                addedEventStars.Add(currentStar);
                continue;
            }

            var user = await _atlasContext.Users.FirstOrDefaultAsync(u => u.Id == starId);
            if (user is null)
                continue;

            EventStar eventStar = new()
            {
                User = user,
                Id = Guid.NewGuid(),
                Status = EventStarStatus.Pending
            };

            // TODO: Publish event to these event stars saying that they've been added.
            addedEventStars.Add(eventStar);
            atlasEvent.Stars.Add(eventStar);
        }

        foreach (var starsToRemove in atlasEvent.Tags.Where(ae => !addedEventTags.Contains(ae)))
        {
            // TODO: Publish event to these event stars saying that they've been removed.
            atlasEvent.Tags.Remove(starsToRemove);
        }    

        await _atlasContext.SaveChangesAsync();

        return atlasEvent;
    }

    public async Task AnnounceEventAsync(Guid id)
    {
        var atlasEvent = await _atlasContext.Events.FirstOrDefaultAsync(e => e.Id == id);
        if (atlasEvent is null || atlasEvent.Status is not EventStatus.Unlisted)
            return;

        atlasEvent.Status = EventStatus.Announced;
        await _atlasContext.SaveChangesAsync();

        // TODO: Publish announcement
    }

    public async Task StartEventAsync(Guid id)
    {
        var atlasEvent = await _atlasContext.Events.FirstOrDefaultAsync(e => e.Id == id);
        if (atlasEvent is null || atlasEvent.Status is not EventStatus.Announced)
            return;

        atlasEvent.Status = EventStatus.Started;
        await _atlasContext.SaveChangesAsync();

        // TODO: Publish event start
    }

    public async Task ConcludeEventAsync(Guid id)
    {
        var atlasEvent = await _atlasContext.Events.FirstOrDefaultAsync(e => e.Id == id);
        if (atlasEvent is null || atlasEvent.Status is not EventStatus.Started)
            return;

        atlasEvent.Status = EventStatus.Concluded;
        await _atlasContext.SaveChangesAsync();

        // TODO: Publish event conclusion
    }

    public async Task CancelEventAsync(Guid id)
    {
        var atlasEvent = await _atlasContext.Events.FirstOrDefaultAsync(e => e.Id == id);
        if (atlasEvent is null || atlasEvent.Status is not EventStatus.Started)
            return;

        atlasEvent.Status = EventStatus.Canceled;
        await _atlasContext.SaveChangesAsync();

        // TODO: Publish event cancellation
    }

    public async Task<Event?> ScheduleEventAsync(Guid id, Instant startTime, Instant? endTime)
    {
        var atlasEvent = await _atlasContext.Events
            .Include(e => e.Tags)
                .ThenInclude(e => e.Tag)
            .Include(e => e.Owner)
            .Include(e => e.Stars)
                .ThenInclude(es => es.User)
            .FirstOrDefaultAsync(e => e.Id == id);

        if (atlasEvent is null)
            return null;

        if (atlasEvent.Status is not EventStatus.Unlisted or EventStatus.Announced)
            return atlasEvent;

        atlasEvent.EndTime = endTime;
        atlasEvent.StartTime = startTime;

        await _atlasContext.SaveChangesAsync();

        var oldTime = atlasEvent.StartTime;
        if (oldTime.HasValue && startTime != oldTime)
        {
            // TODO: Publish rescheduling
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
}