using System.Text.Json.Serialization;

namespace VRAtlas.Models.DTO;

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
}