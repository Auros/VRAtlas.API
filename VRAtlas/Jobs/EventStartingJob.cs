using Microsoft.AspNetCore.OutputCaching;
using Quartz;
using VRAtlas.Logging;
using VRAtlas.Services;

namespace VRAtlas.Jobs;

public class EventStartingJob : IJob
{
    public static readonly JobKey Key = new("event-starting", nameof(VRAtlas));

    private readonly IAtlasLogger _atlasLogger;
    private readonly IEventService _eventService;
    private readonly IOutputCacheStore _outputCacheStore;

    public EventStartingJob(IAtlasLogger<EventStartingJob> atlasLogger, IEventService eventService, IOutputCacheStore outputCacheStore)
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
            _atlasLogger.LogInformation("Automatic event starting job started for {EventId}", eventId);
            var atlasEvent = await _eventService.GetEventByIdAsync(eventId);
            
            // Do not continue if we can't find the event or auto starting is disabled.
            if (atlasEvent is null || !atlasEvent.AutoStart)
            {
                _atlasLogger.LogInformation("Could not find event {EventId}, or auto start is disabled", eventId, atlasEvent?.Status);
                return;
            }    

            await _eventService.StartEventAsync(atlasEvent.Id);
            await _outputCacheStore.EvictByTagAsync("events", default);
            _atlasLogger.LogInformation("Automatic event starting for event {EventId} completed", eventId);
        }
        catch (Exception e) 
        {
            _atlasLogger.LogCritical("An exception occurred while trying to execute the event start job, {Exception}", e);
        }
    }
}