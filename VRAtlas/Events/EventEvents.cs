using VRAtlas.Models;

namespace VRAtlas.Events;

public record struct EventCreatedEvent(Guid Id);
public record struct EventScheduledEvent(Guid Id);
public record struct EventStatusUpdatedEvent(Guid Id, EventStatus Status);