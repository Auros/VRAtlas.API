using VRAtlas.Models;

namespace VRAtlas.Events;

public record class NotificationCreatedEvent(Notification Notification, User User);