﻿using System.Security.Claims;
using VRAtlas.Endpoints.Internal;
using VRAtlas.Models;
using VRAtlas.Services;

namespace VRAtlas.Endpoints;

public class UserEndpoints : IEndpointCollection
{
    public static void BuildEndpoints(IEndpointRouteBuilder app)
    {
        app.MapGet("/user/@me", GetAuthUser)
            .RequireAuthorization()
            .Produces<User>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .WithTags("Users");

        app.MapGet("/user/{id:guid}", GetUserById)
            .Produces<User>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .WithTags("Users");
    }

    private static async Task<IResult> GetAuthUser(IUserService userService, ClaimsPrincipal principal)
    {
        var user = await userService.GetUserAsync(principal);
        if (user is null)
            return Results.Unauthorized();

        return Results.Ok(user);
    }

    private static async Task<IResult> GetUserById(IUserService userService, Guid id)
    {
        var user = await userService.GetUserAsync(id);
        if (user is null)
            return Results.NotFound();

        return Results.Ok(user);
    }
}