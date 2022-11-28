using Microsoft.AspNetCore.Authorization;

namespace VRAtlas.Authorization;

public static class AuthorizationExtensions
{
    public static void AddScopes(this AuthorizationOptions options, string domain, params string[] scopes)
    {
        foreach (var scope in scopes)
            options.AddPolicy(scope, policy => policy.Requirements.Add(new HasScopeRequirement(scope, domain)));
    }
}