using Quartz;
using VRAtlas.Logging;
using VRAtlas.Services;

namespace VRAtlas.Jobs;

public class EventStartingJob : IJob
{
    public static readonly JobKey Key = new("event-starting", nameof(VRAtlas));

    private readonly IAtlasLogger _atlasLogger;
    private readonly IEventService _eventService;

    public EventStartingJob(IAtlasLogger<EventStartingJob> atlasLogger, IEventService eventService)
    {
        _atlasLogger = atlasLogger;
        _eventService = eventService;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        try
        {
            var eventId = Guid.Parse(context.MergedJobDataMap.GetString("Event.Id")!);
            var atlasEvent = await _eventService.GetEventByIdAsync(eventId);
            
            // Do not continue if we can't find the event or auto starting is disabled.
            if (atlasEvent is null || !atlasEvent.AutoStart)
                return;

            await _eventService.StartEventAsync(atlasEvent.Id);
        }
        catch (Exception e) 
        {
            _atlasLogger.LogCritical("An exception occurred while trying to execute the event start job, {Exception}", e);
        }
    }
}