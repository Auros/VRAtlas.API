using Microsoft.EntityFrameworkCore;
using Quartz;
using VRAtlas.Core;
using VRAtlas.Logging;
using VRAtlas.Models;
using VRAtlas.Services;

namespace VRAtlas.Jobs;

public class EventReminderJob : IJob
{
    public static readonly JobKey Key = new("event-reminder", nameof(VRAtlas));

    private readonly IAtlasLogger _atlasLogger;
    private readonly AtlasContext _atlasContext;
    private readonly IEventService _eventService;
    private readonly INotificationService _notificationService;

    public EventReminderJob(IAtlasLogger<EventStartingJob> atlasLogger, AtlasContext atlasContext, IEventService eventService, INotificationService notificationService)
    {
        _atlasLogger = atlasLogger;
        _atlasContext = atlasContext;
        _eventService = eventService;
        _notificationService = notificationService;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        try
        {
            var eventId = Guid.Parse(context.MergedJobDataMap.GetString("Event.Id")!);
            var mode = (EventReminderMode)context.MergedJobDataMap.GetInt("Event.Reminder.Mode");
            _atlasLogger.LogInformation("Automatic event reminder job started for {EventId} for mode {EventReminderMode}", eventId, mode);

            var atlasEvent = await _eventService.GetEventByIdAsync(eventId);

            // Do not continue if we can't find the event.
            if (atlasEvent is null || atlasEvent.Status is not EventStatus.Announced)
            {
                _atlasLogger.LogInformation("Could not find event {EventId}, or it's not announced ({EventStatus})", eventId, atlasEvent?.Status);
                return;
            }    

            // Fetch the user ids of those who follow this event based on the specific event settings.
            IQueryable<Follow> query = _atlasContext.Follows;
            query = mode switch
            {
                EventReminderMode.InThirtyMinutes => query.Where(f => f.EntityType == EntityType.Event && f.EntityId == atlasEvent.Id && f.Metadata.AtThirtyMinutes),
                EventReminderMode.InOneHour => query.Where(f => f.EntityType == EntityType.Event && f.EntityId == atlasEvent.Id && f.Metadata.AtOneHour),
                EventReminderMode.InOneDay => query.Where(f => f.EntityType == EntityType.Event && f.EntityId == atlasEvent.Id && f.Metadata.AtOneDay),
                _ => throw new NotImplementedException(),
            };

            var subscribedUserIds = await query.Select(f => f.UserId).ToArrayAsync();

            var startsIn = mode switch
            {
                EventReminderMode.InThirtyMinutes => "30 minutes",
                EventReminderMode.InOneHour => "one hour",
                EventReminderMode.InOneDay => "one day",
                _ => throw new NotImplementedException(),
            };

            var title = $"{atlasEvent.Name} starts in {startsIn}!";
            var description = $"The event {atlasEvent.Name} hosted by {atlasEvent.Owner!.Name} begins in {startsIn} (subject to change). Hope to see you there!";

            await _notificationService.CreateNotificationAsync(atlasEvent.Id, EntityType.Event, NotificationKeys.EventReminder, title, description, subscribedUserIds);
            _atlasLogger.LogInformation("Automatic event reminder for event {EventId} completed", eventId);
        }
        catch (Exception e)
        {
            _atlasLogger.LogCritical("An exception occurred while trying to execute the event reminder job, {Exception}", e);
        }
    }
}