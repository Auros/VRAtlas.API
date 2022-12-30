using Microsoft.AspNetCore.Authorization;

namespace VRAtlas.Authorization;

public static class AuthorizationExtensions
{
    public static void AddPermissions(this AuthorizationOptions options, string domain, params string[] permissions)
    {
        foreach (var permission in permissions)
            options.AddPolicy(permission, policy => policy.Requirements.Add(new HasPermissionRequirement(permission, domain)));
    }
}