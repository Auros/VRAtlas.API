using Microsoft.AspNetCore.Authorization;

namespace VRAtlas.Authorization;

public class HasPermissionHandler : AuthorizationHandler<HasPermissionRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, HasPermissionRequirement requirement)
    {
        // If user does not have the permission claim, escape.
        if (!context.User.HasClaim(c => c.Type == "permissions" && c.Issuer == requirement.Issuer))
            return Task.CompletedTask;

        // Move all the permissions into their own array
        var permissions = context.User.FindAll(c => c.Type == "permissions" && c.Issuer == requirement.Issuer).Select(c => c.Value);

        // Succeed if the permission array contains the required permission
        if (permissions.Any(s => s == requirement.Permission))
            context.Succeed(requirement);

        return Task.CompletedTask;
    }
}