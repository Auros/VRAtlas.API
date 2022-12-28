using System.Text.Json.Serialization;

namespace VRAtlas.Models;

public class RSVP
{
    [JsonIgnore]
    public Guid Id { get; set; }

    public int? Capacity { get; set; }
}