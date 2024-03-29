﻿using FluentValidation;
using System.Security.Claims;
using VRAtlas.Attributes;
using VRAtlas.Endpoints.Internal;
using VRAtlas.Endpoints.Validators;
using VRAtlas.Models;
using VRAtlas.Models.DTO;
using VRAtlas.Services;

namespace VRAtlas.Endpoints;

public class FollowEndpoints : IEndpointCollection
{
    [VisualName("Follow Entity (Body)")]
    public record FollowEntityBody(Guid Id, EntityType Type, NotificationInfoDTO Metadata);

    [VisualName("Follow Status")]
    public record FollowStatus(bool Status);

    public static void BuildEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/follows");
        group.WithTags("Follows");

        group.MapGet("/{id:guid}", GetAuthFollowStatus)
            .Produces<FollowStatus>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .RequireAuthorization();

        group.MapPost("/", FollowEntity)
            .Produces<FollowDTO>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .RequireAuthorization()
            .AddValidationFilter<FollowEntityBody>();

        group.MapDelete("/{id:guid}", UnfollowEntity)
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status404NotFound)
            .RequireAuthorization();
    }

    public static void AddServices(IServiceCollection services)
    {
        services.AddScoped<IValidator<FollowEntityBody>, FollowEntityBodyValidator>();
    }

    private static async Task<IResult> GetAuthFollowStatus(Guid id, IUserService userService, IFollowService followService, ClaimsPrincipal principal)
    {
        var user = await userService.GetUserAsync(principal);
        if (user is null)
            return Results.Unauthorized();

        var follows = await followService.FollowsAsync(user.Id, id);

        return Results.Ok(new FollowStatus(follows));
    }

    public static async Task<IResult> FollowEntity(FollowEntityBody body, IUserService userService, IFollowService followService, ClaimsPrincipal principal)
    {
        var (id, type, notif) = body;
        var user = await userService.GetUserAsync(principal);
        if (user is null)
            return Results.Unauthorized();

        var follow = await followService.FollowAsync(user.Id, id, type, new NotificationMetadata
        {
            AtThirtyMinutes = notif.AtThirtyMinutes,
            AtStart = notif.AtStart,
            AtOneHour = notif.AtOneHour,
            AtOneDay = notif.AtOneDay,
        });

        return Results.Ok(follow.Map());
    }

    private static async Task<IResult> UnfollowEntity(Guid id, IUserService userService, IFollowService followService, ClaimsPrincipal principal)
    {
        var user = await userService.GetUserAsync(principal);
        if (user is null)
            return Results.Unauthorized();

        var existed = await followService.UnfollowAsync(user.Id, id);
        if (!existed)
            return Results.NotFound();

        return Results.NoContent();
    }
}