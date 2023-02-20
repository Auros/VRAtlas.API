using MessagePipe;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using VRAtlas.Core.Models;

namespace VRAtlas.Core.Services;

internal record ScopedSubscriberInfo<T>(Type Type);

internal class SubscriberHostedService<T> : IHostedService, IMessageHandler<T>
{
    private IDisposable? _disposable;
    private readonly ILogger _logger;
    private readonly ISubscriber<T> _subscriber;
    private readonly IServiceProvider _serviceProvider;
    private readonly IEnumerable<ScopedSubscriberInfo<T>> _scopedSubscriberInfos;

    public SubscriberHostedService(
        ILogger<SubscriberHostedService<T>> logger,
        ISubscriber<T> subscriber,
        IServiceProvider serviceProvider,
        IEnumerable<ScopedSubscriberInfo<T>> scopedSubscriberInfos)
    {
        _logger = logger;
        _subscriber = subscriber;
        _serviceProvider = serviceProvider;
        _scopedSubscriberInfos = scopedSubscriberInfos;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _disposable = _subscriber.Subscribe(this);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _disposable?.Dispose();
        return Task.CompletedTask;
    }

    public void Handle(T message)
    {
        ThreadPool.QueueUserWorkItem(ProcessEvent, message, true);
    }

    private async void ProcessEvent(T message)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            foreach (var info in _scopedSubscriberInfos)
            {
                var instance = ActivatorUtilities.CreateInstance(scope.ServiceProvider, info.Type);
                await (instance as IScopedEventListener<T>)!.Handle(message);
            }
        }
        catch (Exception e)
        {
            _logger.LogCritical(e, "An error occured while proccessing the event {EventType}", typeof(T).FullName);
        }
    }
}