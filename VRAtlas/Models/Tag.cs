using Microsoft.EntityFrameworkCore;
using NodaTime;
using System.ComponentModel.DataAnnotations;

namespace VRAtlas.Models;

[Index(nameof(Id))]
[Index(nameof(Name))]
public class Tag
{
    public Guid Id { get; set; }

    public string Name { get; set; } = null!;

    [Required]
    public User? CreatedBy { get; set; }

    public Instant CreatedAt { get; set; }
}