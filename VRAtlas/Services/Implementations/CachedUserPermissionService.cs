using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;

namespace VRAtlas.Services.Implementations;

/// <summary>
/// Gets the permissions associated with a user. Caches the data into redis.
/// </summary>
public class CachedUserPermissionService : IUserPermissionService
{
    private readonly ILogger _logger;
    private readonly AtlasContext _atlasContext;
    private readonly IConnectionMultiplexer _connectionMultiplexer;

    private const char PermissionSeparator = '|';

    public CachedUserPermissionService(ILogger<CachedUserPermissionService> logger, AtlasContext atlasContext, IConnectionMultiplexer connectionMultiplexer)
    {
        _logger = logger;
        _atlasContext = atlasContext;
        _connectionMultiplexer = connectionMultiplexer;
    }

    public async Task<IEnumerable<string>> GetUserPermissions(Guid userId)
    {
        var userIdString = userId.ToString();
        _logger.LogDebug("Fetching user permissions from cache");
        var database = _connectionMultiplexer.GetDatabase(AtlasConstants.PermissionDatabase);
        var rawPermissions = await database.StringGetAsync(userIdString);
        if (!rawPermissions.HasValue)
        {
            _logger.LogDebug("User {UserId} permissions are not cached, loading from database", userId);
            // If there's no permissions in the redis cache, we load them manually.
            var user = await _atlasContext.Users.AsNoTracking().Include(u => u.Roles).FirstOrDefaultAsync(u => u.Id == userId);
            
            // No user? No permissions.
            if (user is null)
                return Array.Empty<string>();

            // Collect all the unique permissions
            var permissions = user.Roles.SelectMany(r => r.Permissions).Distinct();
            rawPermissions = string.Join(PermissionSeparator, permissions);

            _logger.LogDebug("Adding cached permissions for {UserId} to redis", userId);
            await database.StringSetAsync(userIdString, rawPermissions);
        }

        // Format the permission strings and return the value.
        return rawPermissions.ToString().Split(PermissionSeparator, StringSplitOptions.RemoveEmptyEntries);
    }

    public async Task Clear(Guid userId)
    {
        _logger.LogDebug("Clearing permission keys for {UserId}", userId);
        var database = _connectionMultiplexer.GetDatabase(AtlasConstants.PermissionDatabase);
        await database.KeyDeleteAsync(userId.ToString());
    }

    public async Task ClearAll()
    {
        _logger.LogDebug("Clearing all cached permissions");
        foreach (var server in _connectionMultiplexer.GetServers())
            await server.FlushDatabaseAsync(AtlasConstants.PermissionDatabase);
    }
}