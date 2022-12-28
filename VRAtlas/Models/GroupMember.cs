using NodaTime;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace VRAtlas.Models;

public class GroupMember
{
    [JsonIgnore]
    public Guid Id { get; set; }

    [Required]
    public User? User { get; set; }

    public GroupMemberRole Role { get; set; }

    [JsonIgnore]
    public Instant JoinedAt { get; set; }
}