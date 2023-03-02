using NodaTime;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VRAtlas.Models;

public class GroupMember
{
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    public Group? Group { get; set; }

    [Required]
    public User? User { get; set; }

    public GroupMemberRole Role { get; set; }

    public Instant JoinedAt { get; set; }
}