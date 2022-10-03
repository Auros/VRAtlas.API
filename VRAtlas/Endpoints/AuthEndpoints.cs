using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;

namespace VRAtlas.Endpoints;

public static class AuthEndpoints
{
    internal record ViewableClaim(string Type, string Value);

    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder builder)
    {
        builder.MapGet("/auth/discord", ChallengeDiscordLogin)
            .Produces(StatusCodes.Status308PermanentRedirect);
        builder.MapGet("/auth/claims", ViewUserClaims)
            .Produces<IEnumerable<ViewableClaim>>();
        builder.MapGet("/auth/signout", SignOut);
        return builder;
    }

    internal static async Task<IResult> ChallengeDiscordLogin(IAuthenticationSchemeProvider provider)
    {
        var scheme = await provider.GetSchemeAsync("Discord");
        if (scheme is null) // This really shouldn't happen, but if the server isn't configured properly we should say something.
            return Results.BadRequest("Provider scheme 'Discord' does not exist.");

        // Generate the challenge, this will redirect the user to discord's oauth page.
        return Results.Challenge(new AuthenticationProperties { RedirectUri = "/auth/claims" }, new string[] { scheme.Name });
    }

    internal static IResult ViewUserClaims(ClaimsPrincipal principal)
    {
        // Get the user claims and cast them into a more viewable format
        return Results.Ok(principal.Claims.Select(c => new ViewableClaim(c.Type, c.Value)));
    }

    internal static IResult SignOut()
    {
        return Results.SignOut(new AuthenticationProperties { RedirectUri = "/auth/claims" }, new string[] { CookieAuthenticationDefaults.AuthenticationScheme });
    }
}