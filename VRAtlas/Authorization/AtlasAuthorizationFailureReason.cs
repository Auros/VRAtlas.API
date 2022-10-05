using Microsoft.AspNetCore.Authorization;

namespace VRAtlas.Authorization;

public abstract class AtlasAuthorizationFailureReason : AuthorizationFailureReason
{
    protected AtlasAuthorizationFailureReason(IAuthorizationHandler handler, string message) : base(handler, message)
    {
    }
}