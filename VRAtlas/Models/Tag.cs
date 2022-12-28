using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using NodaTime;
using System.ComponentModel.DataAnnotations;

namespace VRAtlas.Models;

[Index(nameof(Id))]
[Index(nameof(Name))]
public class Tag
{
    [JsonIgnore]
    public Guid Id { get; set; }

    public string Name { get; set; } = null!;

    [Required]
    [JsonIgnore]
    public User? CreatedBy { get; set; }

    [JsonIgnore]
    public Instant CreatedAt { get; set; }
}