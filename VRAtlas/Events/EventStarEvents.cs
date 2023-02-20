namespace VRAtlas.Events;

public record struct EventStarInvitedEvent(Guid EventId, Guid StarId);
public record struct EventStarAcceptedInviteEvent(Guid EventId, Guid StarId);
public record struct EventStarRejectedInviteEvent(Guid EventId, Guid StarId);