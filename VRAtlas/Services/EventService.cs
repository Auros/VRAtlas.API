using Microsoft.EntityFrameworkCore;
using VRAtlas.Models;

namespace VRAtlas.Services;

public interface IEventService
{
    Task<Event?> GetEventByIdAsync(Guid id);
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
}