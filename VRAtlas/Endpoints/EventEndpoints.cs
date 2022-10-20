using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NodaTime;
using System.Security.Claims;
using VRAtlas.Filters;
using VRAtlas.Models;
using VRAtlas.Models.Bodies;
using VRAtlas.Services;
using VRAtlas.Validators;

namespace VRAtlas.Endpoints;

public static class EventEndpoints
{
    public static IServiceCollection ConfigureEventEndpoints(this IServiceCollection services)
    {
        services.AddSingleton<IValidator<ManageEventBody>, ManageEventBodyValidator>();
        return services;
    }

    public static IEndpointRouteBuilder MapEventEndpoints(this IEndpointRouteBuilder builder)
    {
        builder.MapPost("/events/create", CreateEvent)
               .Produces<Event>(StatusCodes.Status200OK)
               .Produces(StatusCodes.Status400BadRequest)
               .Produces(StatusCodes.Status401Unauthorized)
               .Produces(StatusCodes.Status403Forbidden)
               .AddEndpointFilter<ValidationFilter<ManageEventBody>>()
               .RequireAuthorization("CreateEvent");

        return builder;
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