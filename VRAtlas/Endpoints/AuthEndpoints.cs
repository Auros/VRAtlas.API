using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using VRAtlas.Endpoints.Internal;
using VRAtlas.Models;
using VRAtlas.Services;

namespace VRAtlas.Endpoints;

public class AuthEndpoints : IEndpointCollection
{
    public static void AddServices(IServiceCollection services)
    {
        // None
    }

    public static void BuildEndpoints(IEndpointRouteBuilder app)
    {
        app.MapGet("/auth", GetUser)
            .RequireAuthorization();

        app.MapGet("/auth/claims", GetClaims);
        
        app.MapGet("/auth/token", GetUserToken)
            .Produces<UserTokens>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized);
    }

    private static IResult GetUser(HttpContext httpContext)
    {
        _ = httpContext;
        return Results.Ok(new { Hello = "World" });
    }

    private static IResult GetClaims(ClaimsPrincipal principal)
    {
        return Results.Ok(principal.Claims.Select(c => new
        {
            c.Type,
            c.Value
        }));
    }

    private static async Task<IResult> GetUserToken([FromQuery] string code, [FromQuery] Uri redirectUri, IAuthService authService)
    {
        var tokens = await authService.GetUserTokensAsync(code, redirectUri.ToString());
        if (tokens is null)
            return Results.Unauthorized();

        return Results.Ok(tokens);
    }
}