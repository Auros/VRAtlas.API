using Microsoft.EntityFrameworkCore;
using NodaTime;
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
    public required string SocialId { get; set; }

    [JsonIgnore]
    public UserMetadata Metadata { get; set; } = null!;

    [JsonIgnore]
    public required Instant JoinedAt { get; set; }

    [JsonIgnore]
    public required Instant LastLoginAt { get; set; }
}