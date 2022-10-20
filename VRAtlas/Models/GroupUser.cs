using System.Text.Json.Serialization;

namespace VRAtlas.Models;

public class GroupUser
{
    [JsonIgnore]
    public Guid Id { get; set; }
    public User User { get; set; } = null!;
    public GroupRole Role { get; set; }
}