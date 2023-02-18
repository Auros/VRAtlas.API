using Microsoft.EntityFrameworkCore;
using NodaTime;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

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

    [JsonIgnore]
    public string SocialId { get; set; } = null!;

    [Required]
    [JsonIgnore]
    public UserMetadata? Metadata { get; set; }

    [Required]
    [JsonIgnore]
    public NotificationMetadata? DefaultNotificationSettings { get; set; }

    [JsonIgnore]
    public List<Follow> Following { get; set; } = new();

    [JsonIgnore]
    public List<Notification> Notifications { get; set; } = new();

    [JsonIgnore]
    public Instant JoinedAt { get; set; }

    [JsonIgnore]
    public Instant LastLoginAt { get; set; }
}