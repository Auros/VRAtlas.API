using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using VRAtlas.Filters;
using VRAtlas.Models;
using VRAtlas.Models.Bodies;
using VRAtlas.Services;
using VRAtlas.Validators;

namespace VRAtlas.Endpoints;

public static class EventEndpoints
{
    public record EventPage(Event[] Events, Page Page);

    public enum EventStatusCategory
    {
        All,
        Upcoming,
        Current,
        Concluded,
        Canceled
    }

    public static IServiceCollection ConfigureEventEndpoints(this IServiceCollection services)
    {
        services.AddSingleton<IValidator<ManageEventBody>, ManageEventBodyValidator>();
        return services;
    }

    public static IEndpointRouteBuilder MapEventEndpoints(this IEndpointRouteBuilder builder)
    {
        builder.MapGet("/events/{eventId}", GetEvent)
               .Produces<Event>(StatusCodes.Status200OK)
               .Produces(StatusCodes.Status404NotFound);

        builder.MapGet("/events", GetPaginatedEvents)
               .Produces<IEnumerable<Event>>(StatusCodes.Status200OK);

        builder.MapPost("/events/create", CreateEvent)
               .Produces<Event>(StatusCodes.Status200OK)
               .Produces(StatusCodes.Status400BadRequest)
               .Produces(StatusCodes.Status401Unauthorized)
               .Produces(StatusCodes.Status403Forbidden)
               .AddEndpointFilter<ValidationFilter<ManageEventBody>>()
               .RequireAuthorization("CreateEvent");

        return builder;
    }

    private static async Task<IResult> GetEvent(Guid eventId, AtlasContext atlasContext)
    {
        var @event = await atlasContext.Events
            .Include(e => e.Contexts)
            .Include(e => e.Owner)
            .Include(e => e.Stars).ThenInclude(s => s.User).ThenInclude(u => u.Roles)
            .FirstOrDefaultAsync(e => e.Id == eventId);

        if (@event is null)
            return Results.NotFound();

        return Results.Ok(@event);
    }

    internal static async Task<IResult> GetPaginatedEvents(AtlasContext atlasContext, int page = 0, string search = "", EventStatusCategory category = EventStatusCategory.All)
    {
        const int pageSize = 10;

        // If the page number is less than 0, reset it to zero.
        if (0 > page)
            page = 0;

        search = search.ToLower();
        var eventsQuery = atlasContext.Events
            .Where(e => search == string.Empty || e.Name.ToLower().Contains(search) || (e.Description != string.Empty && e.Description.ToLower().Contains(search)))
            .Where(e => e.Stage != StageType.Unlisted);

        if (category is not EventStatusCategory.All)
        {
            switch (category)
            {
                case EventStatusCategory.Upcoming:
                    eventsQuery = eventsQuery.Where(e => e.Stage == StageType.Announced || e.Stage == StageType.Unlisted);
                    break;
                case EventStatusCategory.Current:
                    eventsQuery = eventsQuery.Where(e => e.Stage == StageType.Started);
                    break;
                case EventStatusCategory.Concluded:
                    eventsQuery = eventsQuery.Where(e => e.Stage == StageType.Concluded);
                    break;
                case EventStatusCategory.Canceled:
                    eventsQuery = eventsQuery.Where(e => e.Stage == StageType.Canceled);
                    break;
            }
        }

        var events = await eventsQuery
            .Include(e => e.Contexts)
            .Include(e => e.Owner)
            .Skip(pageSize * page)
            .Take(pageSize)
            .ToArrayAsync();

        var eventCount = await atlasContext.Events.CountAsync();

        // Divide the number of events stored by the size of each page, then get the ceiling to ensure any extra elements get their own page.
        var pageCount = (int)Math.Ceiling(eventCount * 1f / pageSize);

        // Ensure that we have at least one page, even if there's no elements.
        if (pageCount == 0)
            pageCount = 1;

        Page pageInfo = new(page, pageCount);
        EventPage eventPage = new(events, pageInfo);
        return Results.Ok(eventPage);
    }

    private static async Task<IResult> CreateEvent([FromBody] ManageEventBody body, AtlasContext atlasContext, ClaimsPrincipal principal, IVariantCdnService variantCdnService)
    {
        var uploaderIdStr = principal.FindFirstValue(AtlasConstants.IdentifierClaimType);

        var media = await variantCdnService.ValidateAsync(body.MediaImageId, uploaderIdStr);
        if (media is null)
            return Results.BadRequest(new Error { ErrorName = "Invalid Media Id" });

        var group = await atlasContext.Groups.Include(g => g.Users).ThenInclude(gu => gu.User).ThenInclude(u => u.Roles).FirstOrDefaultAsync(g => g.Id == body.GroupId);
        if (group is null)
            return Results.BadRequest(new Error { ErrorName = "Unknown group" });

        var uploaderId = Guid.Parse(uploaderIdStr!);
        if (!group.Users.Any(u => u.User.Id == uploaderId && (u.Role == GroupRole.Owner || u.Role == GroupRole.Manager)))
            return Results.BadRequest(new Error { ErrorName = "Unable to create event in specified group" });

        var stars = (await atlasContext.Users.Where(u => body.Stars.Contains(u.Id)).ToArrayAsync()).Select(u => new EventStar
        {
            Id = Guid.NewGuid(),
            Status = EventStarStatus.Pending,
            User = u
        });

        var contexts = await atlasContext.Contexts.Where(c => body.Contexts.Contains(c.Id)).ToArrayAsync();

        Event @event = new()
        {
            Id = Guid.NewGuid(),
            Name = body.Name,
            Description = body.Description,
            Stage = StageType.Unlisted,
            RSVP = body.RSVP,
            StartTime = body.StartTime,
            EndTime = body.EndTime,
            Media = media,
            Owner = group,
        };

        @event.Stars.AddRange(stars);
        @event.Contexts.AddRange(contexts);

        atlasContext.Events.Add(@event);
        await atlasContext.SaveChangesAsync();
        return Results.Ok(@event);
    }
}