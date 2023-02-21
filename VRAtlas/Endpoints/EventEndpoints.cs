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
    public record class UpdateEventBody(Guid Id, string Name, string Description, Guid? Media, string[] Tags, EventStarInfo[] Stars, bool AutoStart);

    [DisplayName("Schedule Event (Body)")]
    public record class ScheduleEventBody(Guid Id, Instant StartTime, Instant EndTime);

    [DisplayName("Upgrade Event Status (Body)")]
    public record class UpgradeEventBody(Guid Id);

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

        /* TODO: Return specific errors if the event is not in a valid state for these actions. */
        /* With the valid inputs and permissions, it will respond with 204 even if nothing happens. */
        group.MapPut("/announce", AnnounceEvent)
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden)
            .RequireAuthorization("update:events")
            .AddValidationFilter<UpgradeEventBody>();

        group.MapPut("/start", StartEvent)
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden)
            .RequireAuthorization("update:events")
            .AddValidationFilter<UpgradeEventBody>();

        group.MapPut("/conclude", ConcludeEvent)
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden)
            .RequireAuthorization("update:events")
            .AddValidationFilter<UpgradeEventBody>();

        group.MapPut("/cancel", CancelEvent)
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden)
            .RequireAuthorization("update:events")
            .AddValidationFilter<UpgradeEventBody>();
        /* End TODO */
    }

    public static void AddServices(IServiceCollection services)
    {
        services.AddScoped<IValidator<CreateEventBody>, CreateEventBodyValidator>();
        services.AddScoped<IValidator<UpdateEventBody>, UpdateEventBodyValidator>();
        services.AddScoped<IValidator<UpgradeEventBody>, UpgradeEventBodyValidator>();
        services.AddScoped<IValidator<ScheduleEventBody>, ScheduleEventBodyValidator>();
    }

    public static async Task<IResult> GetEventById(IEventService eventService, Guid id)
    {
        var atlasEvent = await eventService.GetEventByIdAsync(id);
        if (atlasEvent is null)
            return Results.NotFound();

        return Results.Ok(atlasEvent);
    }

    public static async Task<IResult> GetEvents(IEventService eventService, Guid? cursor, Guid? group, EventStatus? status, int size = 25)
    {
        var (events, nextCursor, previousCursor) = await eventService.QueryEventsAsync(new()
        {
            Cursor = cursor,
            Group = group,
            Status = status,
            PageSize = size
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

        var (id, name, description, media, tags, stars, autoStart) = body;

        var atlasEvent = await eventService.UpdateEventAsync(id, name, description, media, tags, stars, user.Id, autoStart);

        return Results.Ok(atlasEvent);
    }

    public static async Task<IResult> ScheduleEvent(ScheduleEventBody body, IEventService eventService)
    {
        var (id, startTime, endTime) = body;

        var atlasEvent = await eventService.ScheduleEventAsync(id, startTime, endTime);

        return Results.Ok(atlasEvent);
    }

    public static async Task<IResult> AnnounceEvent(UpgradeEventBody body, IEventService eventService)
    {
        await eventService.AnnounceEventAsync(body.Id);
        return Results.NoContent();
    }

    public static async Task<IResult> StartEvent(UpgradeEventBody body, IEventService eventService)
    {
        await eventService.StartEventAsync(body.Id);
        return Results.NoContent();
    }

    public static async Task<IResult> ConcludeEvent(UpgradeEventBody body, IEventService eventService)
    {
        await eventService.ConcludeEventAsync(body.Id);
        return Results.NoContent();
    }

    public static async Task<IResult> CancelEvent(UpgradeEventBody body, IEventService eventService)
    {
        await eventService.CancelEventAsync(body.Id);
        return Results.NoContent();
    }
}