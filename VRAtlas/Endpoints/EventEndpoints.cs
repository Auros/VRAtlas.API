using FluentValidation;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.EntityFrameworkCore;
using NodaTime;
using System.Security.Claims;
using VRAtlas.Attributes;
using VRAtlas.Endpoints.Internal;
using VRAtlas.Endpoints.Validators;
using VRAtlas.Models;
using VRAtlas.Models.DTO;
using VRAtlas.Services;

namespace VRAtlas.Endpoints;

public class EventEndpoints : IEndpointCollection
{
    [VisualName("Paginated Event Query")]
    public record PaginatedEventQuery(IEnumerable<EventDTO> Events, Guid? Next);

    [VisualName("Create Event (Body)")]
    public record CreateEventBody(string Name, Guid Group, Guid Media);

    [VisualName("Update Event (Body)")]
    public record UpdateEventBody(Guid Id, string Name, string Description, Guid? Media, string[] Tags, EventStarInfo[] Stars, bool AutoStart);

    [VisualName("Schedule Event (Body)")]
    public record ScheduleEventBody(Guid Id, Instant StartTime, Instant EndTime);

    [VisualName("Upgrade Event Status (Body)")]
    public record UpgradeEventBody(Guid Id);

    [VisualName("Star Invitation (Body)")]
    public record StarInvitationBody(Guid Id);

    [VisualName("Event Summaries")]
    public record EventSummaries(IEnumerable<EventDTO> Live, IEnumerable<EventDTO> Upcoming, IEnumerable<EventDTO> Past);

    public static void BuildEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/events");
        group.WithTags("Events");

        group.MapGet("/{id:guid}", GetEventById)
            .Produces<EventDTO>(StatusCodes.Status200OK);

        group.MapGet("/", GetEvents)
            .Produces<PaginatedEventQuery>(StatusCodes.Status200OK)
            .CacheOutput(p => p.Expire(TimeSpan.FromHours(1)).SetVaryByQuery(new string[] { "cursor", "group", "status", "size" }).Tag("events"));

        group.MapPost("/", CreateEvent)
            .Produces<EventDTO>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden)
            .RequireAuthorization("create:events")
            .AddValidationFilter<CreateEventBody>();

        group.MapPut("/", UpdateEvent)
            .Produces<EventDTO>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden)
            .RequireAuthorization("update:events")
            .AddValidationFilter<UpdateEventBody>();

        group.MapPut("/schedule", ScheduleEvent)
            .Produces<EventDTO>(StatusCodes.Status200OK)
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

        group.MapPut("/invite/accept", AcceptEventInvite)
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden)
            .AddValidationFilter<StarInvitationBody>();

        group.MapPut("/invite/reject", RejectEventInvite)
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden)
            .AddValidationFilter<StarInvitationBody>();

        /* End TODO */

        group.MapGet("/summaries", Summaries)
            .Produces<EventSummaries>(StatusCodes.Status200OK)
            .CacheOutput(p => p.Expire(TimeSpan.FromHours(1)).Tag("events"));
    }

    public static void AddServices(IServiceCollection services)
    {
        services.AddScoped<IValidator<CreateEventBody>, CreateEventBodyValidator>();
        services.AddScoped<IValidator<UpdateEventBody>, UpdateEventBodyValidator>();
        services.AddScoped<IValidator<UpgradeEventBody>, UpgradeEventBodyValidator>();
        services.AddScoped<IValidator<ScheduleEventBody>, ScheduleEventBodyValidator>();
        services.AddScoped<IValidator<StarInvitationBody>, StarInvitationBodyValidator>();
    }

    public static async Task<IResult> GetEventById(IEventService eventService, Guid id)
    {
        var atlasEvent = await eventService.GetEventByIdAsync(id);
        if (atlasEvent is null)
            return Results.NotFound();

        return Results.Ok(atlasEvent.Map());
    }

    public static async Task<IResult> GetEvents(IEventService eventService, Guid? cursor, Guid? group, EventStatus? status, int size = 25)
    {
        var (events, nextCursor) = await eventService.QueryEventsAsync(new()
        {
            Cursor = cursor,
            Group = group,
            Status = status,
            PageSize = size
        });
        return Results.Ok(new PaginatedEventQuery(events.Map(), nextCursor));
    }

    public static async Task<IResult> CreateEvent(CreateEventBody body, IEventService eventService)
    {
        var (name, groupId, mediaId) = body;

        var atlasEvent = await eventService.CreateEventAsync(name, groupId, mediaId);

        return Results.Created($"/events/{atlasEvent.Id}", atlasEvent.Map());
    }

    public static async Task<IResult> UpdateEvent(UpdateEventBody body, IUserService userService, IEventService eventService, ClaimsPrincipal principal, IOutputCacheStore cache, CancellationToken token)
    {
        var user = await userService.GetUserAsync(principal);
        if (user is null)
            return Results.Unauthorized();

        var (id, name, description, media, tags, stars, autoStart) = body;

        var atlasEvent = await eventService.UpdateEventAsync(id, name, description, media, tags, stars, user.Id, autoStart);

        await cache.EvictByTagAsync("events", token);

        return Results.Ok(atlasEvent!.Map());
    }

    public static async Task<IResult> ScheduleEvent(ScheduleEventBody body, IEventService eventService, IOutputCacheStore cache, CancellationToken token)
    {
        var (id, startTime, endTime) = body;

        var atlasEvent = await eventService.ScheduleEventAsync(id, startTime, endTime);

        await cache.EvictByTagAsync("events", token);

        return Results.Ok(atlasEvent!.Map());
    }

    public static async Task<IResult> AnnounceEvent(UpgradeEventBody body, IEventService eventService, IOutputCacheStore cache, CancellationToken token)
    {
        await eventService.AnnounceEventAsync(body.Id);
        await cache.EvictByTagAsync("events", token);
        return Results.NoContent();
    }

    public static async Task<IResult> StartEvent(UpgradeEventBody body, IEventService eventService, IOutputCacheStore cache, CancellationToken token)
    {
        await eventService.StartEventAsync(body.Id);
        await cache.EvictByTagAsync("events", token);
        return Results.NoContent();
    }

    public static async Task<IResult> ConcludeEvent(UpgradeEventBody body, IEventService eventService, IOutputCacheStore cache, CancellationToken token)
    {
        await eventService.ConcludeEventAsync(body.Id);
        await cache.EvictByTagAsync("events", token);
        return Results.NoContent();
    }

    public static async Task<IResult> CancelEvent(UpgradeEventBody body, IEventService eventService, IOutputCacheStore cache, CancellationToken token)
    {
        await eventService.CancelEventAsync(body.Id);
        await cache.EvictByTagAsync("events", token);
        return Results.NoContent();
    }

    public static async Task<IResult> AcceptEventInvite(StarInvitationBody body, IUserService userService, IEventService eventService, ClaimsPrincipal principal)
    {
        // Get the current user
        var user = await userService.GetUserAsync(principal);
        if (user is null)
            return Results.Unauthorized();

        await eventService.AcceptStarInvitationAsync(body.Id, user.Id);
        return Results.NoContent();
    }

    public static async Task<IResult> RejectEventInvite(StarInvitationBody body, IUserService userService, IEventService eventService, ClaimsPrincipal principal)
    {
        // Get the current user
        var user = await userService.GetUserAsync(principal);
        if (user is null)
            return Results.Unauthorized();

        await eventService.RejectStarInvitationAsync(body.Id, user.Id);
        return Results.NoContent();
    }

    // A special endpoint for the front page of the website. Heavily cached with event data.
    public static async Task<IResult> Summaries(AtlasContext atlasContext)
    {
        IQueryable<Event> Query(EventStatus status) => atlasContext.Events.Include(e => e.Tags).Where(e => e.Status == status).Take(6);

        var live = await Query(EventStatus.Started).OrderBy(e => e.StartTime).ToArrayAsync();
        var upcoming = await Query(EventStatus.Announced).OrderBy(e => e.StartTime).ToArrayAsync();
        var past = await Query(EventStatus.Concluded).OrderByDescending(e => e.EndTime).ToArrayAsync();

        return Results.Ok(new EventSummaries(live.Map(), upcoming.Map(), past.Map()));
    }
}