using NodaTime;

namespace VRAtlas.Models.Bodies;

public class ManageEventBody
{
    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public Guid GroupId { get; set; }

    public Guid[] Contexts { get; set; } = Array.Empty<Guid>();

    public Guid[] Stars { get; set; } = Array.Empty<Guid>();

    public Instant StartTime { get; set; }

    public Instant EndTime { get; set; }

    public string MediaImageId { get; set; } = string.Empty;

    public RSVP? RSVP { get; set; }
}