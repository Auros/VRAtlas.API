using System.Text.Json.Serialization;
using VRAtlas.Attributes;

namespace VRAtlas.Models.DTO;

[VisualName("Group")]
public class GroupDTO
{
    [JsonPropertyName("id")]
    public required Guid Id { get; init; }

    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [JsonPropertyName("description")]
    public required string Description { get; init; }

    [JsonPropertyName("members")]
    public IEnumerable<GroupMemberDTO> Members { get; init; } = Array.Empty<GroupMemberDTO>();

    [JsonPropertyName("icon")]
    public required Guid Icon { get; init; }

    [JsonPropertyName("banner")]
    public required Guid Banner { get; init; }
}