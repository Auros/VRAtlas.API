using FluentValidation;
using System.ComponentModel;
using VRAtlas.Endpoints.Internal;
using VRAtlas.Endpoints.Validators;
using VRAtlas.Models;
using VRAtlas.Services;

namespace VRAtlas.Endpoints;

public class EventEndpoints : IEndpointCollection
{
    [DisplayName("Paginated Event Query")]
    public record class PaginatedEventQuery(IEnumerable<Event> Events, Guid? Next, Guid? Previous);

    [DisplayName("Create Event (Body)")]
    public record class CreateEventBody(string Name, Guid Group, Guid Media);

    public static void BuildEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/events");
        group.WithTags("Events");

        group.MapGet("/{id:guid}", GetEventById)
            .Produces<Event>(StatusCodes.Status200OK);

        group.MapGet("/", GetEvents)
            .Produces<PaginatedEventQuery>(StatusCodes.Status200OK);

        group.MapPost("/", CreateEvent)
            .Produces(StatusCodes.Status201Created);
    }

    public static void AddServices(IServiceCollection services)
    {
        services.AddScoped<IValidator<CreateEventBody>, CreateEventBodyValidator>();
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

    public static async Task<IResult> CreateEvent(CreateEventBody body, IEventService eventService)
    {
        var (name, groupId, mediaId) = body;

        var atlasEvent = await eventService.CreateEventAsync(name, groupId, mediaId);

        return Results.Created($"/events/{atlasEvent.Id}", atlasEvent);
    }
}