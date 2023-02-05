using System.ComponentModel;

namespace VRAtlas.Models;

[DisplayName("Event Tag")]
public class EventTag
{
    public Guid Id { get; set; }
    public Tag Tag { get; set; } = null!;
    public Event Event { get; set; } = null!;
}