using NodaTime;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace VRAtlas.Models;

[DisplayName("Group Member")]
public class GroupMember
{
    [JsonIgnore]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    [JsonIgnore]
    public Group? Group { get; set; }

    [Required]
    public User? User { get; set; }

    public GroupMemberRole Role { get; set; }

    [JsonIgnore]
    public Instant JoinedAt { get; set; }
}