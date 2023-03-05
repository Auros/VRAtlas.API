using Microsoft.EntityFrameworkCore;
using VRAtlas.Models;

namespace VRAtlas.Services;

public interface IProfileService
{
    public record struct ProfileMetadata(int Followers, int Following);
    public record struct UserCollectionQueryResult(IEnumerable<User> Users, int? NextCursor);
    public record struct GroupCollectionQueryResult(IEnumerable<Group> Groups, int? NextCursor);

    /// <summary>
    /// Gets the follower metadata of a specific user.
    /// </summary>
    /// <param name="userId">The id of the user.</param>
    /// <returns>The profile metadata.</returns>
    Task<ProfileMetadata> GetProfileMetadataAsync(Guid userId);

    /// <summary>
    /// Gets the users that a follow a specific user.
    /// </summary>
    /// <param name="userId">The id of the user.</param>
    /// <param name="cursor">The pagination cursor.</param>
    /// <param name="count">The number of elements to return.</param>
    /// <returns>The result of the query.</returns>
    Task<UserCollectionQueryResult> GetUserFollowersAsync(Guid userId, int? cursor, int count = 25);

    /// <summary>
    /// Gets the users that a specific user follows.
    /// </summary>
    /// <param name="userId">The id of the user.</param>
    /// <param name="cursor">The pagination cursor.</param>
    /// <param name="count">The number of users to return.</param>
    /// <returns>The result of the query.</returns>
    Task<UserCollectionQueryResult> GetUserFollowingAsync(Guid userId, int? cursor, int count = 25);

    /// <summary>
    /// Gets the groups that a specific user follows.
    /// </summary>
    /// <param name="userId">The id of the user.</param>
    /// <param name="cursor">The pagination cursor.</param>
    /// <param name="count">The number of groups to return.</param>
    /// <returns>The result of the query.</returns>
    Task<GroupCollectionQueryResult> GetGroupFollowingAsync(Guid userId, int? cursor, int count = 6);
}

public class ProfileService : IProfileService
{
    private readonly AtlasContext _atlasContext;

    public ProfileService(AtlasContext atlasContext)
    {
        _atlasContext = atlasContext;
    }

    public async Task<IProfileService.ProfileMetadata> GetProfileMetadataAsync(Guid userId)
    {
        var followers = await _atlasContext.Follows.CountAsync(f => f.EntityId == userId);
        var following = await _atlasContext.Follows.CountAsync(f => f.UserId == userId && f.EntityType == EntityType.User);
        return new IProfileService.ProfileMetadata(followers, following);
    }

    public Task<IProfileService.UserCollectionQueryResult> GetUserFollowersAsync(Guid userId, int? cursor, int count = 25)
    {
        IQueryable<Follow> query = _atlasContext.Follows
            .Where(f => f.EntityId == userId)
            .OrderByDescending(f => f.FollowedAt);

        return GetUsersFromQueryAsync(query, cursor, count);
    }

    public Task<IProfileService.UserCollectionQueryResult> GetUserFollowingAsync(Guid userId, int? cursor, int count = 25)
    {
        IQueryable<Follow> query = _atlasContext.Follows
            .Where(f => f.UserId == userId && f.EntityType == EntityType.User)
            .OrderByDescending(f => f.FollowedAt);

        return GetUsersFromQueryAsync(query, cursor, count, true);
    }

    public async Task<IProfileService.GroupCollectionQueryResult> GetGroupFollowingAsync(Guid userId, int? cursor, int count = 6)
    {
        IQueryable<Follow> query = _atlasContext.Follows
            .Where(f => f.UserId == userId && f.EntityType == EntityType.Group)
            .OrderByDescending(f => f.FollowedAt);

        if (cursor.HasValue)
        {
            // Get the start time of the cursor
            var targetTime = await _atlasContext.Follows.Where(f => f.Id == cursor.Value).Select(f => f.FollowedAt).FirstOrDefaultAsync();
            if (targetTime != default)
                query = query.Where(f => targetTime >= f.FollowedAt);
        }

        // Grab an extra element to get the next cursor.
        var follows = await query.Take(count + 1).Select(f => new
        {
            f.Id,
            f.UserId
        }).ToListAsync();

        var followerIds = follows.Select(f => f.UserId).ToList();
        var groups = await _atlasContext.Groups.AsNoTracking().Where(u => followerIds.Contains(u.Id)).ToListAsync();
        int? nextCursor = follows.Count > count ? follows[^1].Id : null;

        // Sort the groups based on the index of the follow order (group query is not guaranteed to returns the groups in order of follow)
        var sortedGroups = groups.OrderBy(u => followerIds.IndexOf(u.Id));

        return new IProfileService.GroupCollectionQueryResult(sortedGroups, nextCursor);
    }

    private async Task<IProfileService.UserCollectionQueryResult> GetUsersFromQueryAsync(IQueryable<Follow> query, int? cursor, int count, bool useEntityId = false)
    {
        if (cursor.HasValue)
        {
            // Get the start time of the cursor
            var targetTime = await _atlasContext.Follows.Where(f => f.Id == cursor.Value).Select(f => f.FollowedAt).FirstOrDefaultAsync();
            if (targetTime != default)
                query = query.Where(f => targetTime >= f.FollowedAt);
        }

        // Grab an extra element to get the next cursor.
        var follows = await query.Take(count + 1).Select(f => new
        {
            f.Id,
            f.UserId,
            f.EntityId
        }).ToListAsync();

        var followerIds = follows.Select(f => useEntityId ? f.EntityId : f.UserId).ToList();
        var users = await _atlasContext.Users.AsNoTracking().Where(u => followerIds.Contains(u.Id)).ToListAsync();
        int? nextCursor = follows.Count > count ? follows[^1].Id : null;

        // Sort the users based on the index of the follow order (user query is not guaranteed to returns the users in order of follow)
        var sortedUsers = users.OrderBy(u => followerIds.IndexOf(u.Id));

        return new IProfileService.UserCollectionQueryResult(sortedUsers, nextCursor);
    }
}