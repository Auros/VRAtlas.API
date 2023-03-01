using NodaTime;
using System.Text.Json.Serialization;

namespace VRAtlas.Models.DTO;

public class NotificationDTO
{
    [JsonPropertyName("id")]
    public required Guid Id { get; init; }

    [JsonPropertyName("userId")]
    public required Guid UserId { get; init; }

    [JsonPropertyName("key")]
    public required string Key { get; init; }

    [JsonPropertyName("title")]
    public required string Title { get; init; }

    [JsonPropertyName("description")]
    public required string Description { get; init; }

    [JsonPropertyName("entityId")]
    public Guid? EntityId { get; init; }

    [JsonPropertyName("entityType")]
    public EntityType? EntityType { get; init; }

    [JsonPropertyName("createdAt")]
    public required Instant CreatedAt { get; init; }

    [JsonPropertyName("read")]
    public required bool Read { get; init; }
}