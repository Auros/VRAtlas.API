using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;
using Microsoft.Extensions.Options;
using VRAtlas.Models.Options;
using static AspNet.Security.OAuth.Discord.DiscordAuthenticationConstants;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace VRAtlas.Endpoints;

public static class AuthEndpoints
{
    internal record ViewableToken(string Token);
    internal record ViewableClaim(string Type, string Value);

    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder builder)
    {
        builder.MapGet("/auth/discord", ChallengeDiscordLogin)
               .Produces(StatusCodes.Status308PermanentRedirect);

        builder.MapGet("/auth/claims", ViewUserClaims)
               .Produces<IEnumerable<ViewableClaim>>();

        builder.MapGet("/auth/signout", SignOut);

        builder.MapPost("/auth/token", GenerateToken)
               .Produces(StatusCodes.Status200OK)
               .Produces(StatusCodes.Status401Unauthorized)
               .RequireAuthorization();

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

    internal static IResult GenerateToken(ClaimsPrincipal principal, IOptions<JwtOptions> options, JwtSecurityTokenHandler handler)
    {
        var jwt = options.Value;
        SymmetricSecurityKey securityKey = new(Encoding.UTF8.GetBytes(jwt.Key));
        SigningCredentials credentials = new(securityKey, SecurityAlgorithms.HmacSha256);

        var lifetime = DateTime.UtcNow.AddHours(jwt.TokenLifetimeInHours);
        JwtSecurityToken secToken = new(issuer: jwt.Issuer, audience: jwt.Audience, principal.Claims, expires: lifetime, signingCredentials: credentials);
        var token = handler.WriteToken(secToken);

        return Results.Ok(new ViewableToken(token));
    }
}