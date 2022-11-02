using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using VRAtlas.Models;
using VRAtlas.Services;

namespace VRAtlas.Authorization;

public static class AtlasExtensions
{
    public static void AddAtlasClaimEvent(this OAuthEvents events)
    {
        // This event adds our custom claims to the user.
        events.OnCreatingTicket = async ctx =>
        {
            // Ensure the user principal isn't null
            // Although it's basically guaranteed to never be null at this state.
            if (ctx.Principal == null)
                return;

            // Get the auth service
            var authService = ctx.HttpContext.RequestServices.GetRequiredService<IAuthService>();

            // Log the user in.
            var user = await authService.GetUserAsync(ctx.Principal);
            if (user is null)
                return;

            // Create our custom identity and assign it to the current user.
            ClaimsIdentity atlasIdentity = new();
            Claim idClaim = new(AtlasConstants.IdentifierClaimType, user.Id.ToString());

            atlasIdentity.AddClaim(idClaim);
            ctx.Principal.AddIdentity(atlasIdentity);
        };
    }

    public static AuthenticationBuilder AddJwtBearer(this AuthenticationBuilder builder, string issuer, string audience, string key)
    {
        return builder.AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters()
            {
                ValidIssuer = issuer,
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidAudience = audience,
                ValidateIssuerSigningKey = true,
                ClockSkew = new TimeSpan(0, 0, 30),
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key))
            };
            options.Events = new JwtBearerEvents()
            {
                OnChallenge = context =>
                {
                    context.HandleResponse();
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    context.Response.ContentType = "application/json";

                    // Ensure we always have an error and error description.
                    if (string.IsNullOrEmpty(context.Error))
                        context.Error = "Invalid Token";
                    if (string.IsNullOrEmpty(context.ErrorDescription))
                        context.ErrorDescription = "This request requires a valid JWT access token to be provided";

                    // Add some extra context for expired tokens.
                    if (context.AuthenticateFailure != null && context.AuthenticateFailure.GetType() == typeof(SecurityTokenExpiredException))
                    {
                        var authenticationException = (context.AuthenticateFailure as SecurityTokenExpiredException)!;
                        context.Response.Headers.Add("x-token-expired", authenticationException.Expires.ToString("o"));
                        context.ErrorDescription = $"The token expired on {authenticationException.Expires:o}";
                    }

                    return context.Response.WriteAsync(JsonSerializer.Serialize(new Error { ErrorName = context.Error }, new JsonSerializerOptions(JsonSerializerDefaults.Web)));
                }
            };
        });
    }
}