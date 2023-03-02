using System.Text.Json.Serialization;
using VRAtlas.Attributes;

namespace VRAtlas.Models.DTO;

[VisualName("Event Star")]
public class EventStarDTO
{
    [JsonPropertyName("user")]
    public required UserDTO User { get; init; }

    [JsonPropertyName("title")]
    public string? Title { get; init; }

    [JsonPropertyName("status")]
    public required EventStarStatus Status { get; init; }
}