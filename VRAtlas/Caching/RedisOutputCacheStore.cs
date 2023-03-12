using Microsoft.AspNetCore.OutputCaching;
using StackExchange.Redis;

namespace VRAtlas.Caching;

public class RedisOutputCacheStore : IOutputCacheStore
{
    private readonly IConnectionMultiplexer _connectionMultiplexer;

    public RedisOutputCacheStore(IConnectionMultiplexer connectionMultiplexer)
    {
        _connectionMultiplexer = connectionMultiplexer;
    }

    public async ValueTask EvictByTagAsync(string tag, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(tag);
        var db = _connectionMultiplexer.GetDatabase();
        var keys = await db.SetMembersAsync(tag);

        var target = keys.Select(x => (RedisKey)x.ToString())
            .Concat(new[] { (RedisKey)tag })
            .ToArray();

        await db.KeyDeleteAsync(target);
    }

    public async ValueTask<byte[]?> GetAsync(string key, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(key);
        var db = _connectionMultiplexer.GetDatabase();
        return await db.StringGetAsync(key); 
    }

    public async ValueTask SetAsync(string key, byte[] value, string[]? tags, TimeSpan validFor, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(value);

        if (tags is null)
            return;
        
        var db = _connectionMultiplexer.GetDatabase();
        foreach (var tag in tags)
            await db.SetAddAsync(tag, key);

        await db.StringSetAsync(key, value, validFor);
    }
}