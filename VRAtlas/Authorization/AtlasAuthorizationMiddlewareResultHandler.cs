using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using VRAtlas.Models;

namespace VRAtlas.Authorization;

/// <summary>
/// Wow, what a mouthful
/// </summary>
public class AtlasAuthorizationMiddlewareResultHandler : IAuthorizationMiddlewareResultHandler
{
    private readonly AuthorizationMiddlewareResultHandler defaultHandler = new();

    public async Task HandleAsync(RequestDelegate next, HttpContext context, AuthorizationPolicy policy, PolicyAuthorizationResult authorizeResult)
    {
        var atlasFailures = authorizeResult.AuthorizationFailure?.FailureReasons.Where(fr => fr is AtlasAuthorizationFailureReason);

        // If the authorization has no errors specific to us, we don't care.
        if (atlasFailures == null || !atlasFailures.Any())
        {
            await defaultHandler.HandleAsync(next, context, policy, authorizeResult);
            return;
        }

        var missingPermissionReason = atlasFailures.FirstOrDefault(fr => fr is AtlasAuthorizationFailureReason);
        if (missingPermissionReason is AtlasMissingPermissionsAuthorizationFailureReason missingPermissionFailureReason)
        {
            MissingPermissions permissions = new(missingPermissionFailureReason.MissingPermissions);
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsJsonAsync(permissions);
        }
    }
}