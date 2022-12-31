using Microsoft.EntityFrameworkCore;
using NodaTime;
using VRAtlas.Logging;
using VRAtlas.Models;

namespace VRAtlas.Services;

public interface IGroupService
{
    /// <summary>
    /// Gets a group by it's id.
    /// </summary>
    /// <param name="id">The id of the group.</param>
    /// <returns>The group, if it exists.</returns>
    Task<Group?> GetGroupByIdAsync(Guid id);

    /// <summary>
    /// Gets all the groups that a user is in.
    /// </summary>
    /// <param name="userId">The id of the user.</param>
    /// <returns>A collection of the groups that the user is in.</returns>
    Task<IEnumerable<Group>> GetAllUserGroupsAsync(Guid userId);

    /// <summary>
    /// Creates a group.
    /// </summary>
    /// <param name="name">The name of the group.</param>
    /// <param name="description">The description of the group.</param>
    /// <param name="icon">The icon of the group.</param>
    /// <param name="banner">The banner of the group.</param>
    /// <param name="ownerId">The owner of the group's id.</param>
    /// <returns>The created group.</returns>
    Task<Group> CreateGroupAsync(string name, string description, Guid icon, Guid banner, Guid ownerId);

    /// <summary>
    /// Adds a member to a group. If the user is already in the group and the role provided is different from their current, it will change the role.
    /// </summary>
    /// <param name="id">The id of the group.</param>
    /// <param name="userId">The user id of the member to add.</param>
    /// <param name="role">The role to assign the member.</param>
    /// <returns>The updated group.</returns>
    Task<Group> AddGroupMemberAsync(Guid id, Guid userId, GroupMemberRole role);

    /// <summary>
    /// Removes a member from a group.
    /// </summary>
    /// <param name="id">The id of the group.</param>
    /// <param name="userId">The id of the user to remove.</param>
    /// <returns>The updated group.</returns>
    Task<Group> RemoveGroupMemberAsync(Guid id, Guid userId);

    /// <summary>
    /// Checks if a group exists.
    /// </summary>
    /// <param name="id">The id of the group.</param>
    /// <returns>Returns true if the group exists.</returns>
    Task<bool> GroupExistsAsync(Guid id);

    /// <summary>
    /// Checks if a group exists by its name.
    /// </summary>
    /// <param name="name">The name of the group.</param>
    /// <returns>Returns true if the group exists.</returns>
    Task<bool> GroupExistsByNameAsync(string name);

    /// <summary>
    /// Gets a user's role in a specific group.
    /// </summary>
    /// <param name="id">The id of the group.</param>
    /// <param name="userId">The id of the user.</param>
    /// <returns>The role of the group member. If they are not in the group, null.</returns>
    Task<GroupMemberRole?> GetGroupMemberRoleAsync(Guid id, Guid userId);

    /// <summary>
    /// Modifies an existing group
    /// </summary>
    /// <param name="id">The id of the group.</param>
    /// <param name="description">The description of the group.</param>
    /// <param name="icon">The id of the group icon resource.</param>
    /// <param name="banner">The id of the group banner resource.</param>
    /// <returns>The updated group.</returns>
    Task<Group> ModifyGroupAsync(Guid id, string description, Guid icon, Guid banner);
}

public class GroupService : IGroupService
{
    private readonly IClock _clock;
    private readonly IAtlasLogger _atlasLogger;
    private readonly IUserService _userService;
    private readonly AtlasContext _atlasContext;

    public GroupService(IClock clock, IAtlasLogger<GroupService> atlasLogger, IUserService userService, AtlasContext atlasContext)
    {
        _clock = clock;
        _atlasLogger = atlasLogger;
        _userService = userService;
        _atlasContext = atlasContext;
    }

    public async Task<Group> AddGroupMemberAsync(Guid id, Guid userId, GroupMemberRole role)
    {
        if (role == GroupMemberRole.Owner)
            throw new InvalidOperationException("Cannot add or modify a member to the Owner role");

        var group = await _atlasContext.Groups.Include(g => g.Members).ThenInclude(g => g.User).FirstOrDefaultAsync(g => g.Id == id);
        if (group is null)
        {
            _atlasLogger.LogWarning("Tried to modify a group member {UserId} in a group that doesn't exist {GroupId}", userId, id);
            throw new InvalidOperationException($"Could not find group with id '{id}'.");
        }

        // Check if the member is in the group already.
        var member = group.Members.FirstOrDefault(m => m.User!.Id == userId);
        if (member is null)
        {
            // The member is not in the group.
            var user = await _userService.GetUserAsync(userId);
            if (user is null)
                return group; // Return the group as is, like there were no changes.

            // Create the new group member object
            var now = _clock.GetCurrentInstant();
            group.Members.Add(new()
            {
                Id = userId,
                User = user,
                Role = role,
                JoinedAt = now
            });
        }
        else
        {
            // The member is in the group.
            // Ensure we're not removing the owner
            if (member.Role is GroupMemberRole.Owner)
                throw new InvalidOperationException("Cannot modify the role of the group owner.");

            // Update the role
            member.Role = role;
        }

        await _atlasContext.SaveChangesAsync();
        _atlasLogger.LogInformation("User {UserId} was added to group {GroupId}", userId, id);
        return group;
    }

    public async Task<Group> CreateGroupAsync(string name, string description, Guid icon, Guid banner, Guid ownerId)
    {
        // Ensure that the group name is unique.
        var groupNameExists = await _atlasContext.Groups.AnyAsync(g => g.Name.ToLower() == name.ToLower());
        if (groupNameExists)
        {
            _atlasLogger.LogWarning("A group creation event was attempted with an already existing name of {GroupName}", name);
            throw new InvalidOperationException($"Group name '{name}' already exists");
        }

        // Ensure that the owner attached to this group exists.
        var owner = await _userService.GetUserAsync(ownerId);
        if (owner is null)
        {
            _atlasLogger.LogWarning("Unable to create a group, could not find the owning user");
            throw new InvalidOperationException($"A user with the id '{ownerId}' does not exist.");
        }

        // Get the current time used for storing the time the owner "joins" and when the group was created.
        var now = _clock.GetCurrentInstant();

        // Construct the group object
        Group group = new()
        {
            Id = Guid.NewGuid(),
            Name = name,
            Description = description,
            Icon = icon,
            Banner = banner,
            CreatedAt = now,
            Members = new()
            {
                new GroupMember
                {
                    Id = Guid.NewGuid(),
                    User = owner,
                    JoinedAt = now,
                    Role = GroupMemberRole.Owner,
                }
            },
        };

        _atlasContext.Groups.Add(group);
        await _atlasContext.SaveChangesAsync();
        _atlasLogger.LogInformation("User {UserId} created group {GroupId}", ownerId, group.Id);
        return group;
    }

    public async Task<IEnumerable<Group>> GetAllUserGroupsAsync(Guid userId)
    {
        var groups = await _atlasContext.Groups
            .AsNoTracking()
            .Where(g => g.Members.Any(m => m.User!.Id == userId))
            .Include(g => g.Members)
                .ThenInclude(m => m.User)
            .ToListAsync();

        // Sort the groups to have the groups owned by the user first, then the groups they manage, then the groups they're just in.
        groups.Sort((groupA, groupB) =>
        {
            var roleInGroupA = groupA.Members.First(m => m.User!.Id == userId).Role;
            var roleInGroupB = groupB.Members.First(m => m.User!.Id == userId).Role;
            return roleInGroupB - roleInGroupA;
        });

        return groups;
    }

    public Task<Group?> GetGroupByIdAsync(Guid id)
    {
        return _atlasContext.Groups
            .AsNoTracking()
            .Include(g => g.Members)
                .ThenInclude(m => m.User)
            .FirstOrDefaultAsync();
    }

    public async Task<GroupMemberRole?> GetGroupMemberRoleAsync(Guid id, Guid userId)
    {
        // Unfortunately, we need to return the GroupMember here as we cannot return a nullable value type within the query in this context.
        var member = await _atlasContext.Groups
            .Where(g => g.Id == id && g.Members.Any(m => m.User!.Id == userId)).Select(g => g.Members.First(m => m.User!.Id == userId))
            .FirstOrDefaultAsync();

        return member?.Role;
    }

    public Task<bool> GroupExistsAsync(Guid id)
    {
        return _atlasContext.Groups.AnyAsync(g => g.Id == id);
    }

    public Task<bool> GroupExistsByNameAsync(string name)
    {
        return _atlasContext.Groups.AnyAsync(g => g.Name.ToLower() == name.ToLower());
    }

    public async Task<Group> ModifyGroupAsync(Guid id, string description, Guid icon, Guid banner)
    {
        var group = await _atlasContext.Groups.FirstOrDefaultAsync(g => g.Id == id);
        if (group is null)
        {
            _atlasLogger.LogWarning("Tried to modify a group that does not exist, {GroupId}", id);
            throw new InvalidOperationException($"Could not find group with id '{id}'.");
        }

        group.Icon = icon;
        group.Banner = banner;
        group.Description = description;

        await _atlasContext.SaveChangesAsync();
        _atlasLogger.LogInformation("Group {GroupId} was updated", id);

        // We don't return the group object above since we never loaded the group members in the query.
        return (await GetGroupByIdAsync(group.Id))!;
    }

    public async Task<Group> RemoveGroupMemberAsync(Guid id, Guid userId)
    {
        var group = await _atlasContext.Groups.Include(g => g.Members).ThenInclude(g => g.User).FirstOrDefaultAsync(g => g.Id == id);
        if (group is null)
        {
            _atlasLogger.LogWarning("Tried to remove a group member {UserId} in a group that doesn't exist {GroupId}", userId, id);
            throw new InvalidOperationException($"Could not find group with id '{id}'.");
        }

        var member = group.Members.FirstOrDefault(m => m.User!.Id == userId);
        if (member?.Role == GroupMemberRole.Owner)
        {
            _atlasLogger.LogWarning("Tried to remove the owner from the group {GroupId}", id);
            throw new InvalidOperationException("Cannot add or modify a member to the Owner role");
        }
        
        if (member is not null)
            group.Members.Remove(member);

        await _atlasContext.SaveChangesAsync();
        _atlasLogger.LogInformation("User {UserId} was removed from group {GroupId}", userId, id);
        return group;
    }
}