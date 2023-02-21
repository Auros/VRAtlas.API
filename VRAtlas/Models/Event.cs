using Microsoft.EntityFrameworkCore;
using NodaTime;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace VRAtlas.Models;

[Index(nameof(Id))]
public class Event
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    [Required]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Group? Owner { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<EventStar> Stars { get; set; } = null!;

    public EventStatus Status { get; set; }

    public Instant? StartTime { get; set; }

    public Instant? EndTime { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<EventTag> Tags { get; set; } = null!;

    public Guid Media { get; set; }

    public bool AutoStart { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public RSVP? RSVP { get; set; }
}