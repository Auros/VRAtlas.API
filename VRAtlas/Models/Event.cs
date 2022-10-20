using Microsoft.EntityFrameworkCore;
using NodaTime;
using System.ComponentModel.DataAnnotations.Schema;

namespace VRAtlas.Models;

[Index(nameof(Id))]
public class Event
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public Group Owner { get; set; } = null!;

    public List<Context> Contexts { get; set; } = new();

    public List<EventStar> Stars { get; set; } = new();

    public StageType Stage { get; set; }
    
    public Instant StartTime { get; set; }

    public Instant EndTime { get; set; }

    [Column(TypeName = "jsonb")]
    public ImageVariants Media { get; set; } = null!;

    [Column(TypeName = "jsonb")]
    public RSVP? RSVP { get; set; }
}