using System.Text.Json.Serialization;
using VRAtlas.Attributes;

namespace VRAtlas.Models.DTO;

[VisualName("User")]
public class UserDTO
{
    [JsonPropertyName("id")]
    public required Guid Id { get; init; }

    [JsonPropertyName("username")]
    public required string Username { get; init; }

    [JsonPropertyName("picture")]
    public required Guid Picture { get; init; }

    [JsonPropertyName("biography")]
    public string? Biography { get; init; }

    [JsonPropertyName("links")]
    public IEnumerable<string> Links { get; init; } = Enumerable.Empty<string>();
}