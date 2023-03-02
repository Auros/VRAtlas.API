using VRAtlas.Attributes;

namespace VRAtlas.Models;

[VisualName("Event Star Info")]
public class EventStarInfo
{
    public Guid Star { get; set; }
    public string? Title { get; set; }
}