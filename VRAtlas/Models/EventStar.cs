using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace VRAtlas.Models;

public class EventStar
{
    [JsonIgnore]
    public Guid Id { get; set; }

    [Required]
    public User? User { get; set; }

    public EventStarStatus Status { get; set; }
}