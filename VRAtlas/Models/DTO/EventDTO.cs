using NodaTime;
using System.Text.Json.Serialization;
using VRAtlas.Attributes;

namespace VRAtlas.Models.DTO;

[VisualName("Event")]
public class EventDTO
{
    [JsonPropertyName("id")]
    public required Guid Id { get; init; }

    [JsonPropertyName("name")]
    public required string Name { get; init; } = string.Empty;

    [JsonPropertyName("description")]
    public required string Description { get; init; } = string.Empty;

    [JsonPropertyName("owner")]
    public GroupDTO? Owner { get; init; }

    [JsonPropertyName("stars")]
    public IEnumerable<EventStarDTO> Stars { get; init; } = Array.Empty<EventStarDTO>();

    [JsonPropertyName("status")]
    public required EventStatus Status { get; init; }

    [JsonPropertyName("startTime")]
    public Instant? StartTime { get; init; }

    [JsonPropertyName("endTime")]
    public Instant? EndTime { get; init; }

    [JsonPropertyName("tags")]
    public IEnumerable<string> Tags { get; init; } = Array.Empty<string>();

    [JsonPropertyName("media")]
    public required Guid Media { get; init; }

    [JsonPropertyName("autoStart")]
    public required bool AutoStart { get; init; }
}