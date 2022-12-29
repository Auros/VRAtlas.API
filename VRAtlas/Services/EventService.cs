using Microsoft.EntityFrameworkCore;
using VRAtlas.Models;
using static VRAtlas.Services.IEventService;

namespace VRAtlas.Services;

public interface IEventService
{
    Task<Event?> GetEventByIdAsync(Guid id);

    public record struct EventCollectionQueryOptions(Guid? Cursor, int PageSize = 25);
    public record struct EventCollectionQueryResult(IEnumerable<Event> Events, Guid? NextCursor, Guid? PreviousCursor);
    Task<EventCollectionQueryResult> QueryEventsAsync(EventCollectionQueryOptions options);
}

public class EventService : IEventService
{
    private readonly AtlasContext _atlasContext;

    public EventService(AtlasContext atlasContext)
    {
        _atlasContext = atlasContext;
    }

    public Task<Event?> GetEventByIdAsync(Guid id)
    {
        return _atlasContext.Events
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
}