using NodaTime;
using System.ComponentModel.DataAnnotations;

namespace VRAtlas.Models;

public class WebPushSubscription
{
    [Key]
    public string Endpoint { get; set; } = null!;

    public string P256DH { get; set; } = null!;

    public string Auth { get; set; } = null!;

    public Guid UserId { get; set; }

    public Instant CreatedAt { get; set; }
}