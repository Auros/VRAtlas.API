using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using VRAtlas.Models;
using VRAtlas.Services;

namespace VRAtlas.Endpoints;

public static class UserEndpoints
{
    public static IEndpointRouteBuilder MapUserEndpoints(this IEndpointRouteBuilder builder)
    {
        builder.MapGet("/users/@me", GetLoggedInUser)
            .Produces<User>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .RequireAuthorization();

        builder.MapGet("/user/{userId}", GetUserById);

        return builder;
    }

    private static async Task<IResult> GetUserById(Guid userId, AtlasContext atlasContext)
    {
        var user = await atlasContext.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
        return user is not null ? Results.Ok(user) : Results.NotFound();
    }

    private static async Task<IResult> GetLoggedInUser(ClaimsPrincipal principal, IAuthService authService)
    {
        var user = await authService.GetUserAsync(principal);
        return user is not null ? Results.Ok(user) : Results.NotFound();
    }
}