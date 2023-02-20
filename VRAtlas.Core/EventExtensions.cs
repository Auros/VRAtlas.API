using Microsoft.Extensions.DependencyInjection;
using VRAtlas.Core.Models;
using VRAtlas.Core.Services;

namespace VRAtlas.Core;

public static class EventExtensions
{
    public static IServiceCollection AddScopedEventListener<TEvent, TEventListener>(this IServiceCollection services) where TEventListener : IScopedEventListener<TEvent>
    {
        // Check if the subscriber hosted service has been registered to reuse the scope if there are multiple events.
        if (!services.Any(s => s.ImplementationType == typeof(SubscriberHostedService<TEvent>)))
            services.AddHostedService<SubscriberHostedService<TEvent>>();

        services.AddSingleton(_ => new ScopedSubscriberInfo<TEvent>(typeof(TEventListener)));

        return services;
    }
}