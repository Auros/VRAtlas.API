using Microsoft.EntityFrameworkCore;
using NodaTime;
using System.ComponentModel.DataAnnotations;

namespace VRAtlas.Models;

[Index(nameof(Id))]
public class Event
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    [Required]
    public Group? Owner { get; set; }

    public List<EventStar> Stars { get; set; } = null!;

    public EventStatus Status { get; set; }

    public Instant? StartTime { get; set; }

    public Instant? EndTime { get; set; }

    public List<EventTag> Tags { get; set; } = null!;

    public Guid Media { get; set; }

    public Guid? Video { get; set; }

    public bool AutoStart { get; set; }

    public RSVP? RSVP { get; set; }
}