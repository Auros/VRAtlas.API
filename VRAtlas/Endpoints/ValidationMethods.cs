using VRAtlas.Models;
using VRAtlas.Services;

namespace VRAtlas.Endpoints;

public static class ValidationMethods
{
    public static async Task<bool> EnsureValidImageAsync(Guid resourceId, IHttpContextAccessor httpContextAccessor, IUserService userService, IImageCdnService imageCdnService)
    {
        // Get the currently authenticated user.
        var principal = httpContextAccessor.HttpContext?.User;
        if (principal is null)
            return false;

        // Ensure that they're a user within our database.
        var user = await userService.GetUserAsync(principal);
        if (user is null)
            return false;

        // Validate that the image exists.
        return await imageCdnService.ValidateAsync(resourceId, user.Id);
    }

    public static async Task<bool> EnsureUserCanUpdateGroupAsync(Guid id, IHttpContextAccessor httpContextAccessor, IUserService userService, IGroupService groupService)
    {
        // Get the currently authenticated user.
        var principal = httpContextAccessor.HttpContext?.User;
        if (principal is null)
            return false;

        // Ensure that they're a user within our database.
        var user = await userService.GetUserAsync(principal);
        if (user is null)
            return false;

        // Ensure that the user has the valid permissions to modify the group.
        var role = await groupService.GetGroupMemberRoleAsync(id, user.Id);
        return role is GroupMemberRole.Owner || role is GroupMemberRole.Manager;
    }
}