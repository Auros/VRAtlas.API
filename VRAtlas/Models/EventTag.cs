using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VRAtlas.Models;

public class EventTag
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    public Tag Tag { get; set; } = null!;

    public Event Event { get; set; } = null!;
}