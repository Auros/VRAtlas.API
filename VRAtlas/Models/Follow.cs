using NodaTime;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace VRAtlas.Models;

public class Follow
{
    [JsonIgnore]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    public Guid UserId { get; set; }

    public Guid EntityId { get; set; }

    public EntityType EntityType { get; set; }

    public Instant FollowedAt { get; set; }

    public NotificationMetadata Metadata { get; set; } = null!;
}