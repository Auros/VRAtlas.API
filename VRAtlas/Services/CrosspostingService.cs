using VRAtlas.Models;
using VRAtlas.Models.Crossposters;

namespace VRAtlas.Services;

public interface ICrosspostingService
{
    /// <summary>
    /// Gets, updates, and or creates the crossposter's group.
    /// </summary>
    /// <param name="source">The group source info.</param>
    /// <returns>The crossposter's group.</returns>
    Task<Group> GetCrossposterGroupAsync(CrosspostSource source);
}

public class CrosspostingService : ICrosspostingService
{
    private readonly IGroupService _groupService;

    public CrosspostingService(IGroupService groupService)
    {
        _groupService = groupService;
    }

    public async Task<Group> GetCrossposterGroupAsync(CrosspostSource source)
    {
        var group = await _groupService.GetGroupByNameAsync(source.Name);
        
        // In the case this crossposter source has an overlapping name with a group that was created by a user, reject the system from this operation.
        // This should not happen if the application is configured properly.
        if (group is not null && group.Identity is null)
            throw new InvalidOperationException("Cannot override an existing group not created by the system.");

        if (group is null)
            group = await _groupService.CreateGroupAsync(source.Name, source.Description ?? string.Empty, source.Icon, source.Banner, null, source.Source.ToString());
        else
            group = await _groupService.ModifyGroupAsync(group.Id, source.Description ?? string.Empty, source.Icon, source.Banner);

        return group;
    }
}
