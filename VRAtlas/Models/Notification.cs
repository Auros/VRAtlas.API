using NodaTime;
using System.ComponentModel.DataAnnotations.Schema;

namespace VRAtlas.Models;

public class Notification
{
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public string Key { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public Guid? EntityId { get; set; }

    public EntityType? EntityType { get; set; }

    public Instant CreatedAt { get; set; }

    public bool Read { get; set; }
}