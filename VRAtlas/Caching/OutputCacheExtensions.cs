using Microsoft.AspNetCore.OutputCaching;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace VRAtlas.Caching;

public static class OutputCacheExtensions
{
    public static IServiceCollection AddRedisOutputCache(this IServiceCollection services, Action<OutputCacheOptions>? options = null)
    {
        if (options is not null)
        {
            services.Configure(options);
            services.AddOutputCache(options);
        }
        else
        {
            services.AddOutputCache();
        }
        services.RemoveAll<IOutputCacheStore>();
        services.AddSingleton<IOutputCacheStore, RedisOutputCacheStore>();
        return services;
    }
}