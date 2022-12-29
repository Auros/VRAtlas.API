using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VRAtlas.Models;

public class EventStar
{
    [JsonIgnore]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Guid Id { get; set; }

    [Required]
    public User? User { get; set; }

    public EventStarStatus Status { get; set; }
}