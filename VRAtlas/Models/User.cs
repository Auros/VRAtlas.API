using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace VRAtlas.Models;

public class User
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    [JsonIgnore, Column(TypeName = "jsonb")]
    public PlatformIdentifiers Identifiers { get; set; } = new();

    [Column(TypeName = "jsonb")]
    public ImageVariants Icon { get; set; } = new();

    [JsonIgnore]
    public string? IconSourceUrl { get; set; }

    [JsonIgnore]
    public string? Email { get; set; }
}