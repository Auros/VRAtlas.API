using VRAtlas.Endpoints.Internal;
using VRAtlas.Models;
using VRAtlas.Services;

namespace VRAtlas.Endpoints;

public class EventEndpoints : IEndpointCollection
{
    public static void BuildEndpoints(IEndpointRouteBuilder app)
    {
        app.MapGet("/event/{id:guid}", GetEventById)
            .Produces<Event>(StatusCodes.Status200OK)
            .WithTags("Events");

        app.MapGet("/events", GetEvents)
            .Produces<PaginatedEventQuery>(StatusCodes.Status200OK)
            .WithTags("Events");
    }

    public static async Task<IResult> GetEventById(IEventService eventService, Guid id)
    {
        var atlasEvent = await eventService.GetEventByIdAsync(id);
        if (atlasEvent is null)
            return Results.NotFound();

        return Results.Ok(atlasEvent);
    }

    public static async Task<IResult> GetEvents(IEventService eventService, Guid? cursor)
    {
        var (events, nextCursor, previousCursor) = await eventService.QueryEventsAsync(new()
        {
            Cursor = cursor
        });
        return Results.Ok(new PaginatedEventQuery(events, nextCursor, previousCursor));
    }

    public record class PaginatedEventQuery(IEnumerable<Event> Events, Guid? Next, Guid? Previous);
}