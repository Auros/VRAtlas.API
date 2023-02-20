using VRAtlas.Core.Models;
using VRAtlas.Events;
using VRAtlas.Models;

namespace VRAtlas.Listeners;

public class EventAnnouncementListener : IScopedEventListener<EventStatusUpdatedEvent>
{
    public Task Handle(EventStatusUpdatedEvent message)
    {
        if (message.Status is not EventStatus.Announced)
            return Task.CompletedTask;

        return Task.FromResult(message.Status);
    }
}