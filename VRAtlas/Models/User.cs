using Microsoft.EntityFrameworkCore;
using NodaTime;
using System.ComponentModel.DataAnnotations;

namespace VRAtlas.Models;

[Index(nameof(Id))]
[Index(nameof(SocialId))]
public class User
{
    public required Guid Id { get; set; }

    public required string Username { get; set; }

    public Guid Picture { get; set; } 

    public string? Biography { get; set; }

    public List<string> Links { get; set; } = new();

    public string SocialId { get; set; } = null!;

    public ProfileStatus ProfileStatus { get; set; }

    [Required]
    public UserMetadata? Metadata { get; set; }

    [Required]
    public NotificationMetadata? DefaultNotificationSettings { get; set; }

    public List<Follow> Following { get; set; } = new();

    public List<Notification> Notifications { get; set; } = new();

    public Instant JoinedAt { get; set; }

    public Instant LastLoginAt { get; set; }
}