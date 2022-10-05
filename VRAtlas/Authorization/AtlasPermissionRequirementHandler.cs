using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using VRAtlas.Services;

namespace VRAtlas.Authorization;

public class AtlasPermissionRequirementHandler : AuthorizationHandler<AtlasPermissionRequirement>
{
    private readonly ILogger _logger;
    private readonly IUserPermissionService _userPermissionService;

    public AtlasPermissionRequirementHandler(ILogger<AtlasPermissionRequirementHandler> logger, IUserPermissionService userPermissionService)
    {
        _logger = logger;
        _userPermissionService = userPermissionService;
    }

    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, AtlasPermissionRequirement requirement)
    {
        // Ensure that the claim has a valid identifier claim type.
        var userIdStr = context.User.FindFirstValue(AtlasConstants.IdentifierClaimType);
        if (userIdStr is null || !Guid.TryParse(userIdStr, out var userId))
            return;

        var permissions = await _userPermissionService.GetUserPermissions(userId);
        var missingPermissions = requirement.Permissions.Where(p => !permissions.Contains(p));

        // Check if there are missing permissions and it isn't an administrator.
        if (missingPermissions.Any() && !permissions.Contains(AtlasConstants.SpecialAdministrator))
        {
            _logger.LogInformation("Permission checked failed for {UserId}", userId);
            AtlasMissingPermissionsAuthorizationFailureReason reason = new(this, missingPermissions);
            context.Fail(reason);
            return;
        }

        _logger.LogDebug("Permission check succeeded for {UserId}", userId);
        context.Succeed(requirement);
    }
}