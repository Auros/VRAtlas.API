namespace VRAtlas.Models;

public class EventTag
{
    public Guid Id { get; set; }
    public Tag Tag { get; set; } = null!;
    public Event Event { get; set; } = null!;
}