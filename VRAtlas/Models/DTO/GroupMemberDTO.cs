using System.Text.Json.Serialization;

namespace VRAtlas.Models.DTO;

public class GroupMemberDTO
{
    [JsonPropertyName("user")]
    public required UserDTO User { get; init; }

    [JsonPropertyName("role")]
    public required GroupMemberRole Role { get; init; }
}