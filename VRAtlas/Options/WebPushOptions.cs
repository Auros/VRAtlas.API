using System.ComponentModel.DataAnnotations;

namespace VRAtlas.Options;

public class WebPushOptions
{
    public const string Name = "WebPush";

    [Required]
    public string PublicKey { get; set; } = string.Empty;

    [Required]
    public string PrivateKey { get; set; } = string.Empty;
}