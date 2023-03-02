using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VRAtlas.Models;

public class EventStar
{
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    public User? User { get; set; }

    public EventStarStatus Status { get; set; }

    public string? Title { get; set; }
}