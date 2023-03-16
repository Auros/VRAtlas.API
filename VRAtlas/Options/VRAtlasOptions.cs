using System.ComponentModel.DataAnnotations;

namespace VRAtlas.Options;

public class VRAtlasOptions
{
    public const string Name = "VRAtlas";

    [Required]
    public required string Salt { get; set; }

    [Required]
    public required string CdnPath { get; set; }

    public long MaximumFileSizeLength { get; set; } = 1_000_000;
}