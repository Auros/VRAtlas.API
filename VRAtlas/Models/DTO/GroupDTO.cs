using System.Text.Json.Serialization;

namespace VRAtlas.Models.DTO;

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
}
