using Microsoft.AspNetCore.Authorization;

namespace VRAtlas.Authorization;

public class HasPermissionRequirement : IAuthorizationRequirement
{
    public string Issuer { get; }
    public string Permission { get; }

    public HasPermissionRequirement(string permission, string issuer)
    {
        Issuer = issuer ?? throw new ArgumentNullException(nameof(issuer));
        Permission = permission ?? throw new ArgumentNullException(nameof(permission));
    }
}