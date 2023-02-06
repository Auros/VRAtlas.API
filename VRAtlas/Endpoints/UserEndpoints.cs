using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using VRAtlas.Endpoints.Internal;
using VRAtlas.Models;
using VRAtlas.Services;

namespace VRAtlas.Endpoints;

public class UserEndpoints : IEndpointCollection
{
    public static void BuildEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/users");
        group.WithTags("Users");

        group.MapGet("/@me", GetAuthUser)
            .Produces<User>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .RequireAuthorization();

        group.MapGet("/{id:guid}", GetUserById)
            .Produces<User>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        group.MapGet("/search", SearchForUsers)
            .Produces<IEnumerable<User>>(StatusCodes.Status200OK);
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

    private static async Task<IResult> SearchForUsers(IUserService userService, [FromQuery] string? query = null)
    {
        // If the search parameter has nothing.
        if (string.IsNullOrWhiteSpace(query))
            return Results.Ok(Array.Empty<User>());

        var users = await userService.GetUsersAsync(query);

        return Results.Ok(users);
    }
}