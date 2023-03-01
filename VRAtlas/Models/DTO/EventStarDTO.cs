using System.Text.Json.Serialization;

namespace VRAtlas.Models.DTO;

public class EventStarDTO
{
    [JsonPropertyName("user")]
    public required UserDTO User { get; init; }

    [JsonPropertyName("title")]
    public string? Title { get; init; }

    [JsonPropertyName("status")]
    public required EventStarStatus Status { get; init; }
}