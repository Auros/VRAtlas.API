using MessagePipe;
using Microsoft.EntityFrameworkCore;
using NodaTime;
using VRAtlas.Events;
using VRAtlas.Logging;
using VRAtlas.Models;

namespace VRAtlas.Services;

public interface INotificationService
{
    /// <summary>
    /// Checks if a notification exists.
    /// </summary>
    /// <param name="id">The id of the notification to check.</param>
    /// <returns>Does the notification exist?</returns>
    Task<bool> ExistsAsync(Guid id);

    /// <summary>
    /// Marks a notification as "read".
    /// </summary>
    /// <param name="id">The id of the notification</param>
    /// <returns>If the notification was successfully marked as read (false if the notification doesn't exist).</returns>
    Task<bool> MarkAsReadAsync(Guid id);

    /// <summary>
    /// Gets a collection of notifications.
    /// </summary>
    /// <param name="userId">The id of the user who received the notification.</param>
    /// <param name="cursor">The cursor to start at.</param>
    /// <param name="isRead">Only return read notifications if true, only return unread notifications if false, return both if null.</param>
    /// <param name="count">The maximum number of elements to include in the query.</param>
    /// <returns>The notification query.</returns>
    Task<NotificationCollectionQueryResult> QueryNotificationsAsync(Guid userId, Guid? cursor, bool? isRead, int count = 5);
    public record struct NotificationCollectionQueryResult(IEnumerable<Notification> Notifications, Guid? NextCursor, int Unread);
    
    /// <summary>
    /// Creates a notification.
    /// </summary>
    /// <param name="entityId">The id of the subject entity being notified about.</param>
    /// <param name="entityType">The type of the entity being notified about.</param>
    /// <param name="key">The notification key.</param>
    /// <param name="title">The title of the notification.</param>
    /// <param name="contentTemplate">The content template of the notification.</param>
    /// <param name="targets">The targets to send the notification to.</param>
    /// <returns></returns>
    Task CreateNotificationAsync(Guid entityId, EntityType entityType, string key, string title, string contentTemplate, params Guid[] targets);

}

public class NotificationService : INotificationService
{
    private readonly IClock _clock;
    private readonly IAtlasLogger _atlasLogger;
    private readonly AtlasContext _atlasContext;
    private readonly IPublisher<NotificationCreatedEvent> _notificationCreated;

    public NotificationService(IClock clock, IAtlasLogger<NotificationService> atlasLogger, AtlasContext atlasContext, IPublisher<NotificationCreatedEvent> notificationCreated)
    {
        _clock = clock;
        _atlasLogger = atlasLogger;
        _atlasContext = atlasContext;
        _notificationCreated = notificationCreated;
    }

    public async Task<bool> MarkAsReadAsync(Guid id)
    {
        var notif = await _atlasContext.Notifications.FindAsync(id);
        if (notif is null)
            return false;
        
        notif.Read = true;
        await _atlasContext.SaveChangesAsync();
        return true;
    }

    public async Task CreateNotificationAsync(Guid entityId, EntityType entityType, string key, string title, string contentTemplate, params Guid[] targets)
    {
        List<(Notification, User)> addedNotifications = new();
        foreach (var target in targets)
        {
            var user = await _atlasContext.Users.FindAsync(target);
            
            if (user is null)
                continue;

            var content = contentTemplate.Replace("{User.Username}", user.Username);
            
            Notification notif = new()
            {
                Key = key,
                Title = title,
                UserId = user.Id,
                EntityId = entityId,
                Description = content,
                EntityType = entityType,
                CreatedAt = _clock.GetCurrentInstant()
            };

            _atlasContext.Notifications.Add(notif);
            addedNotifications.Add((notif, user));
        }

        await _atlasContext.SaveChangesAsync();
        _atlasLogger.LogInformation("Created notification for {EntityId} ({EntityType}) with key {NotificationKey}", entityId, entityType, key);

        foreach (var (notif, user) in addedNotifications)
            _notificationCreated.Publish(new NotificationCreatedEvent(notif, user));
    }

    public async Task<INotificationService.NotificationCollectionQueryResult> QueryNotificationsAsync(Guid userId, Guid? cursor, bool? isRead, int count = 5)
    {
        var unread = await _atlasContext.Notifications.CountAsync(n => n.UserId == userId && !n.Read);

        // Clamp the query if necessary
        count = Math.Clamp(count, 1, 50);

        IQueryable<Notification> query = _atlasContext.Notifications.AsNoTracking().Where(n => n.UserId == userId).OrderByDescending(n => n.CreatedAt);
        if (cursor.HasValue)
        {
            // Get the start time of the cursor
            var targetTime = await _atlasContext.Notifications.Where(n => n.Id == cursor.Value).Select(n => n.CreatedAt).FirstOrDefaultAsync();
            if (targetTime != default)
                query = query.Where(n => targetTime >= n.CreatedAt);
        }

        if (isRead.HasValue)
            query = query.Where(n => n.Read == isRead.Value);

        // Grab an extra element to get the next cursor.
        var notifications = await query.Take(count + 1).ToListAsync();

        Guid? nextCursor = notifications.Count > count ? notifications[^1].Id : null;
        if (nextCursor.HasValue)
            notifications.RemoveAt(notifications.Count - 1); // Remove the last element since we've got it's id for the next cursor.

        INotificationService.NotificationCollectionQueryResult result = new(notifications, nextCursor, unread);
        return result;
    }

    public Task<bool> ExistsAsync(Guid id)
    {
        return _atlasContext.Notifications.AnyAsync(n => n.Id == id);
    }
}