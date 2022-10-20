using System.Text.Json.Serialization;

namespace VRAtlas.Models;

public class EventStar
{
    [JsonIgnore]
    public Guid Id { get; set; }

    public User User { get; set; } = null!;

    public EventStarStatus Status { get; set; }
}