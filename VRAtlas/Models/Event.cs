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
    public Group? Owner { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<EventStar>? Stars { get; set; }

    public EventStatus Status { get; set; }

    public Instant StartTime { get; set; }

    public Instant EndTime { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<Tag>? Tags { get; set; }

    public Guid Media { get; set; }

    public RSVP? RSVP { get; set; }
}