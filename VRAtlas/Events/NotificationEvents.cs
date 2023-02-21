using VRAtlas.Models;

namespace VRAtlas.Events;

public record NotificationCreatedEvent(Notification Notification, User User);