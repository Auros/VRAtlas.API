using System.ComponentModel.DataAnnotations;

namespace VRAtlas.Options;

public class CloudflareOptions
{
    public const string Name = "Cloudflare";

    [Required]
    public required Uri ApiUrl { get; set; }

    [Required]
    public required string ApiKey { get; set; }
}