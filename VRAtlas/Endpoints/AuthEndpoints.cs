using Microsoft.AspNetCore.Mvc;
using VRAtlas.Endpoints.Internal;
using VRAtlas.Models;
using VRAtlas.Services;

namespace VRAtlas.Endpoints;

public class AuthEndpoints : IEndpointCollection
{
    public static void BuildEndpoints(IEndpointRouteBuilder app)
    {
        app.MapGet("/auth/token", GetUserToken)
            .Produces<UserTokens>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .WithTags("Auth");
    }

    private static async Task<IResult> GetUserToken([FromQuery] string code, [FromQuery] Uri redirectUri, IAuthService authService, IUserGrantService userGrantService)
    {
        var tokens = await authService.GetUserTokensAsync(code, redirectUri.ToString());
        if (tokens is null)
            return Results.Unauthorized();

        // Create or update the user within the database
        // This only happens when they request new tokens.
        // This will cause the profile picture to re-update if necessary,
        // change the username if necessary, and more. Doing this call here
        // also means we don't need to query Auth0 for the user info as
        // all the information we need is stored within the id token, which
        // the decryption key is our Auth0 Client Secret since we use HS256
        await userGrantService.GrantUserAsync(tokens);

        return Results.Ok(tokens);
    }
}