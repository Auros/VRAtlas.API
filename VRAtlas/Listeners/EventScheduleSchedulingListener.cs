﻿using NodaTime;
using Quartz;
using VRAtlas.Core.Models;
using VRAtlas.Events;
using VRAtlas.Jobs;
using VRAtlas.Logging;
using VRAtlas.Models;
using VRAtlas.Services;

namespace VRAtlas.Listeners;

public class EventScheduleSchedulingListener : IScopedEventListener<EventScheduledEvent>
{
    private readonly IClock _clock;
    private readonly IAtlasLogger _logger;
    private readonly IEventService _eventService;
    private readonly ISchedulerFactory _schedulerFactory;

    public EventScheduleSchedulingListener(IClock clock, IAtlasLogger<EventScheduleSchedulingListener> logger, IEventService eventService, ISchedulerFactory schedulerFactory)
    {
        _clock = clock;
        _logger = logger;
        _eventService = eventService;
        _schedulerFactory = schedulerFactory;
    }

    public async Task Handle(EventScheduledEvent message)
    {
        var id = message.Id;

        // Get the event from the subject
        var atlasEvent = await _eventService.GetEventByIdAsync(message.Id) ?? throw new Exception($"Unable to find event with id {message.Id}. This should not happen.");

        if (!atlasEvent.StartTime.HasValue || !atlasEvent.EndTime.HasValue)
            return;

        var scheduler = await _schedulerFactory.GetScheduler();

        JobDataMap eventDataMap = new();
        eventDataMap.Put("Event.Id", atlasEvent.Id);

        TriggerKey endingKey = new($"event-ending.{id}", nameof(VRAtlas));
        TriggerKey startingKey = new($"event-starting.{id}", nameof(VRAtlas));
        TriggerKey reminderOneDayKey = new($"event-reminder.{id}.OneDay", nameof(VRAtlas));
        TriggerKey reminderOneHourKey = new($"event-reminder.{id}.OneHour", nameof(VRAtlas));
        TriggerKey reminderThirtyMinutesKey = new($"event-reminder.{id}.ThirtyMinutes", nameof(VRAtlas));

        // Unschedule all existing jobs that may have existed before.
        await scheduler.UnscheduleJobs(new[]
        {
            endingKey,
            startingKey,
            reminderOneDayKey,
            reminderOneHourKey,
            reminderThirtyMinutesKey
        });

        // Create the event triggers
        List<ITrigger> triggers = new()
        {
            TriggerBuilder.Create()
                .WithIdentity(startingKey)
                .StartAt(atlasEvent.StartTime.Value.ToDateTimeUtc())
                .ForJob(EventStartingJob.Key)
                .UsingJobData(eventDataMap)
                .Build(),

            TriggerBuilder.Create()
                .WithIdentity(endingKey)
                .StartAt(atlasEvent.EndTime.Value.ToDateTimeUtc())
                .ForJob(EventEndingJob.Key)
                .UsingJobData(eventDataMap)
                .Build()
        };

        var now = _clock.GetCurrentInstant();
        if (atlasEvent.StartTime.Value > now.Plus(Duration.FromMinutes(30)))
        {
            triggers.Add(TriggerBuilder.Create()
                .WithIdentity(reminderThirtyMinutesKey)
                .StartAt(atlasEvent.StartTime.Value.Minus(Duration.FromMinutes(30)).ToDateTimeUtc())
                .ForJob(EventReminderJob.Key)
                .UsingJobData(new JobDataMap { { "Event.Id", atlasEvent.Id }, { "Event.Reminder.Mode", (int)EventReminderMode.InThirtyMinutes } })
                .Build());
        }
        if (atlasEvent.StartTime.Value > now.Plus(Duration.FromHours(1)))
        {
            triggers.Add(TriggerBuilder.Create()
                .WithIdentity(reminderOneHourKey)
                .StartAt(atlasEvent.StartTime.Value.Minus(Duration.FromHours(1)).ToDateTimeUtc())
                .ForJob(EventReminderJob.Key)
                .UsingJobData(new JobDataMap { { "Event.Id", atlasEvent.Id }, { "Event.Reminder.Mode", (int)EventReminderMode.InOneHour } })
                .Build());
        }
        if (atlasEvent.StartTime.Value > now.Plus(Duration.FromDays(1)))
        {
            triggers.Add(TriggerBuilder.Create()
                .WithIdentity(reminderOneDayKey)
                .StartAt(atlasEvent.StartTime.Value.Minus(Duration.FromDays(1)).ToDateTimeUtc())
                .ForJob(EventReminderJob.Key)
                .UsingJobData(new JobDataMap { { "Event.Id", atlasEvent.Id }, { "Event.Reminder.Mode", (int)EventReminderMode.InOneDay } })
                .Build());
        }

        // Schedule the event triggers
        foreach (var trigger in triggers)
        {
            _logger.LogInformation("Scheduling Trigger {TriggerName} for Event {AtlasEventId}", trigger.Key.Name, atlasEvent.Id);
            await scheduler.ScheduleJob(trigger);
        }
            
    }
}