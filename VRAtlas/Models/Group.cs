using Microsoft.EntityFrameworkCore;
using NodaTime;

namespace VRAtlas.Models;

[Index(nameof(Id))]
public class Group
{
    public Guid Id { get; set; }

    public string Name { get; set; } = null!;

    public string Description { get; set; } = null!;

    public Guid Icon { get; set; }

    public Guid Banner { get; set; }

    public Instant CreatedAt { get; set; }
}