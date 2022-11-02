using System.Security.Claims;
using Microsoft.Extensions.Options;
using VRAtlas.Models.Options;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using VRAtlas.Models.Bodies;
using VRAtlas.Services;
using VRAtlas.Models;

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

        builder.MapPost("/auth/token", GenerateToken)
               .Produces(StatusCodes.Status200OK)
               .Produces(StatusCodes.Status400BadRequest);

        return builder;
    }

    internal static IResult ChallengeDiscordLogin(IOptions<DiscordOptions> options)
    {
        var discord = options.Value;
        var redirectTo = $"{AtlasConstants.DiscordApiUrl}/oauth2/authorize?response_type=code&client_id={discord.ClientId}&scope=identify email&redirect_uri={discord.RedirectUrl}";
        return Results.Redirect(redirectTo, preserveMethod: false);
    }

    internal static IResult ViewUserClaims(ClaimsPrincipal principal)
    {
        // Get the user claims and cast them into a more viewable format
        return Results.Ok(principal.Claims.Select(c => new ViewableClaim(c.Type, c.Value)));
    }

    internal static async Task<IResult> GenerateToken(IOptions<JwtOptions> options, JwtSecurityTokenHandler handler, IDiscordService discordService, IAuthService authService, [FromBody] CodeBody body)
    {
        var accessToken = await discordService.GetAccessTokenAsync(body.Code);
        if (accessToken is null)
            return Results.BadRequest(new Error { ErrorName = "Invalid code" });

        var profile = await discordService.GetProfileAsync(accessToken);
        if (profile is null)
            return Results.BadRequest(new Error { ErrorName = "Could not find profile" });

        List<Claim> claims = new()
        {
            new Claim(ClaimTypes.Name, profile.Username),
            new Claim(ClaimTypes.NameIdentifier, profile.Id),
            new Claim("urn:discord:avatar:hash", profile.Avatar),
            new Claim("urn:discord:user:discriminator", profile.Discriminator),
        };

        if (profile.Email is not null)
            claims.Add(new Claim(ClaimTypes.Email, profile.Email));

        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims));
        var user = await authService.GetUserAsync(principal);

        if (user is null)
            return Results.BadRequest(new Error { ErrorName = "Could not acquire authentication requirements" });

        claims.Add(new Claim(AtlasConstants.IdentifierClaimType, user.Id.ToString()));

        var jwt = options.Value;
        SymmetricSecurityKey securityKey = new(Encoding.UTF8.GetBytes(jwt.Key));
        SigningCredentials credentials = new(securityKey, SecurityAlgorithms.HmacSha256);

        var lifetime = DateTime.UtcNow.AddHours(jwt.TokenLifetimeInHours);
        JwtSecurityToken secToken = new(issuer: jwt.Issuer, audience: jwt.Audience, claims, expires: lifetime, signingCredentials: credentials);
        var token = handler.WriteToken(secToken);

        return Results.Ok(new ViewableToken(token));
    }
}