using Newtonsoft.Json;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VRAtlas.Models;

[DisplayName("Event Star")]
public class EventStar
{
    [JsonIgnore]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    public User? User { get; set; }

    public EventStarStatus Status { get; set; }

    public string? Title { get; set; }
}