using System.Text.Json.Serialization;
using VRAtlas.Attributes;

namespace VRAtlas.Models.DTO;

[VisualName("Group Member")]
public class GroupMemberDTO
{
    [JsonPropertyName("user")]
    public required UserDTO User { get; init; }

    [JsonPropertyName("role")]
    public required GroupMemberRole Role { get; init; }
}