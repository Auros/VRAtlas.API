using System.ComponentModel.DataAnnotations;

namespace VRAtlas.Models.Crossposters;

public class CrosspostSource
{
    [Required]
    public required string Name { get; set; }

    [Required]
    public required Guid Icon { get; set; }

    [Required]
    public required Guid Banner { get; set; }

    [Required]
    public required Uri Source { get; set; }

    public string? Description { get; set; }
}