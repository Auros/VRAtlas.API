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

    [JsonIgnore]
    public string SocialId { get; set; } = null!;

    [Required]
    [JsonIgnore]
    public UserMetadata? Metadata { get; set; }

    [JsonIgnore]
    public Instant JoinedAt { get; set; }

    [JsonIgnore]
    public Instant LastLoginAt { get; set; }
}