using NodaTime;
using System.Text.Json.Serialization;
using VRAtlas.Attributes;

namespace VRAtlas.Models.DTO;

[VisualName("Follow")]
public class FollowDTO
{
    [JsonPropertyName("userId")]
    public required Guid UserId { get; init; }

    [JsonPropertyName("entityId")]
    public required Guid EntityId { get; init; }

    [JsonPropertyName("entityType")]
    public required EntityType EntityType { get; init; }

    [JsonPropertyName("followedAt")]
    public required Instant FollowedAt { get; init; }

    [JsonPropertyName("metadata")]
    public required NotificationInfoDTO Metadata { get; init; }
}