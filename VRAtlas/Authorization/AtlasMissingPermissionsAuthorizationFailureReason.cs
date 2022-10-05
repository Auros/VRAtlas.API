using Microsoft.AspNetCore.Authorization;

namespace VRAtlas.Authorization;

public class AtlasMissingPermissionsAuthorizationFailureReason : AtlasAuthorizationFailureReason
{
    public IReadOnlyList<string> MissingPermissions { get; set; }

    public AtlasMissingPermissionsAuthorizationFailureReason(IAuthorizationHandler handler, IEnumerable<string> missingPermissions)
        : base(handler, "Missing permissions(s) " + missingPermissions.Aggregate((a, b) => $"{a}, {b}"))
    {
        MissingPermissions = missingPermissions.ToArray();
    }
}