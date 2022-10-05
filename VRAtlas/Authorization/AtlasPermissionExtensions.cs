using Microsoft.AspNetCore.Authentication.OAuth;
using System.Security.Claims;
using VRAtlas.Services;

namespace VRAtlas.Authorization;

public static class AtlasPermissionExtensions
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
}