using Microsoft.EntityFrameworkCore;
using NodaTime;
using VRAtlas.Logging;
using VRAtlas.Models;

namespace VRAtlas.Services;

public interface IFollowService
{
    /// <summary>
    /// Checks if a user follows an entity.
    /// </summary>
    /// <param name="userId">The id of the user.</param>
    /// <param name="entityId">The entity to check if the provided user is following.</param>
    /// <returns>Does the specified user follow this entity?</returns>
    Task<bool> FollowsAsync(Guid userId, Guid entityId);

    /// <summary>
    /// Follows an entity. Will edit the notification metadata if it already exists.
    /// </summary>
    /// <param name="userId">The id of the user who wants to follow the entity.</param>
    /// <param name="entityId">The id of the entity to follow.</param>
    /// <param name="entityType">The type of the target entity.</param>
    /// <param name="metadata">The notification metadata associated with this follow.</param>
    /// <returns></returns>
    Task<Follow> FollowAsync(Guid userId, Guid entityId, EntityType entityType, NotificationMetadata metadata);

    /// <summary>
    /// Unfollows an entity.
    /// </summary>
    /// <param name="userid">The id of the user that wants to unfollow the specified entity.</param>
    /// <param name="entityId">The id of the entity to unfollow.</param>
    /// <returns>If the follow was removed (will be false if it never existed).</returns>
    Task<bool> UnfollowAsync(Guid userId, Guid entityId);
}

public class FollowService : IFollowService
{
    private readonly IClock _clock;
    private readonly IAtlasLogger _atlasLogger;
    private readonly AtlasContext _atlasContext;

    public FollowService(IClock clock, IAtlasLogger<FollowService> atlasLogger, AtlasContext atlasContext)
    {
        _clock = clock;
        _atlasLogger = atlasLogger;
        _atlasContext = atlasContext;
    }

    public Task<bool> FollowsAsync(Guid userId, Guid entityId)
    {
        return _atlasContext.Follows.AnyAsync(f => f.UserId == userId && f.EntityId == entityId);
    }

    public async Task<Follow> FollowAsync(Guid userId, Guid entityId, EntityType entityType, NotificationMetadata metadata)
    {
        var follow = await _atlasContext.Follows.Include(f => f.Metadata).FirstOrDefaultAsync(f => f.UserId == userId && f.EntityId == entityId);
        if (follow is null)
        {
            follow = new Follow
            {
                UserId = userId,
                EntityId = entityId,
                EntityType = entityType,
                Metadata = new NotificationMetadata()
            };
            _atlasContext.Follows.Add(follow);
        }

        follow.Metadata.AtStart = metadata.AtStart;
        follow.Metadata.AtThirtyMinutes = metadata.AtThirtyMinutes;
        follow.Metadata.AtOneHour = metadata.AtOneHour;
        follow.Metadata.AtOneDay = metadata.AtOneDay;

        follow.FollowedAt = _clock.GetCurrentInstant();

        await _atlasContext.SaveChangesAsync();
        _atlasLogger.LogInformation("User {UserId} successfully followed the entity {EntityId} ({EntityType})", userId, entityId, entityType);

        return follow;
    }

    public async Task<bool> UnfollowAsync(Guid userId, Guid entityId)
    {
        var follow = await _atlasContext.Follows.FirstOrDefaultAsync(f => f.UserId == userId && f.EntityId == entityId);
        if (follow is null)
            return false;

        _atlasContext.Follows.Remove(follow);
        await _atlasContext.SaveChangesAsync();
        _atlasLogger.LogInformation("User {UserId} successfully unfollowed the entity {EntityId}", userId, entityId);
        return true;
    }
}