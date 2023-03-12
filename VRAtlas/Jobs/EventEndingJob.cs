using Microsoft.AspNetCore.OutputCaching;
using Quartz;
using VRAtlas.Logging;
using VRAtlas.Services;

namespace VRAtlas.Jobs;

public class EventEndingJob : IJob
{
    public static readonly JobKey Key = new("event-ending", nameof(VRAtlas));

    private readonly IAtlasLogger _atlasLogger;
    private readonly IEventService _eventService;
    private readonly IOutputCacheStore _outputCacheStore;

    public EventEndingJob(IAtlasLogger<EventEndingJob> atlasLogger, IEventService eventService, IOutputCacheStore outputCacheStore)
    {
        _atlasLogger = atlasLogger;
        _eventService = eventService;
        _outputCacheStore = outputCacheStore;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        try
        {
            var eventId = Guid.Parse(context.MergedJobDataMap.GetString("Event.Id")!);
            _atlasLogger.LogInformation("Automatic event ending job started for {EventId}", eventId);
            var atlasEvent = await _eventService.GetEventByIdAsync(eventId);

            // Do not continue if we can't find the event.
            if (atlasEvent is null)
                return;

            // Conclude the event.
            await _eventService.ConcludeEventAsync(atlasEvent.Id);
            await _outputCacheStore.EvictByTagAsync("events", default);
            _atlasLogger.LogInformation("Automatic event ending for event {EventId} completed", eventId);
        }
        catch (Exception e)
        {
            _atlasLogger.LogCritical("An exception occurred while trying to execute the event end job, {Exception}", e);
        }
    }
}