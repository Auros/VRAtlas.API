using FluentValidation;
using NodaTime;
using System.ComponentModel;
using System.Security.Claims;
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

    [DisplayName("Update Event (Body)")]
    public record class UpdateEventBody(Guid Id, string Name, string Description, Guid Media, IEnumerable<string> Tags, IEnumerable<Guid> Stars);

    [DisplayName("Schedule Event (Body)")]
    public record class ScheduleEventBody(Guid Id, Instant StartTime, Instant? EndTime);

    public static void BuildEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/events");
        group.WithTags("Events");

        group.MapGet("/{id:guid}", GetEventById)
            .Produces<Event>(StatusCodes.Status200OK);

        group.MapGet("/", GetEvents)
            .Produces<PaginatedEventQuery>(StatusCodes.Status200OK);

        group.MapPost("/", CreateEvent)
            .Produces<Event>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden)
            .RequireAuthorization("create:events")
            .AddValidationFilter<CreateEventBody>();

        group.MapPut("/", UpdateEvent)
            .Produces<Event>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden)
            .RequireAuthorization("update:events")
            .AddValidationFilter<UpdateEventBody>();

        group.MapPut("/schedule", ScheduleEvent)
            .Produces<Event>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden)
            .RequireAuthorization("update:events")
            .AddValidationFilter<ScheduleEventBody>();
    }

    public static void AddServices(IServiceCollection services)
    {
        services.AddScoped<IValidator<CreateEventBody>, CreateEventBodyValidator>();
        services.AddScoped<IValidator<UpdateEventBody>, UpdateEventBodyValidator>();
        services.AddScoped<IValidator<ScheduleEventBody>, ScheduleEventBodyValidator>();
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

    public static async Task<IResult> UpdateEvent(UpdateEventBody body, IUserService userService, IEventService eventService, ClaimsPrincipal principal)
    {
        var user = await userService.GetUserAsync(principal);
        if (user is null)
            return Results.Unauthorized();

        var (id, name, description, media, tags, stars) = body;

        var atlasEvent = await eventService.UpdateEventAsync(id, name, description, media, tags, stars, user.Id);

        return Results.Ok(atlasEvent);
    }

    public static async Task<IResult> ScheduleEvent(ScheduleEventBody body, IEventService eventService)
    {
        var (id, startTime, endTime) = body;

        var atlasEvent = await eventService.ScheduleEventAsync(id, startTime, endTime);

        return Results.Ok(atlasEvent);
    }
}