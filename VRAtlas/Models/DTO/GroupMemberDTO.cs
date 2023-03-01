using System.Text.Json.Serialization;

namespace VRAtlas.Models.DTO;

public class GroupMemberDTO
{
    [JsonPropertyName("group")]
    public required GroupDTO Group { get; init; }

    [JsonPropertyName("user")]
    public required User User { get; init; }

    [JsonPropertyName("role")]
    public required GroupMemberRole Role { get; init; }
}