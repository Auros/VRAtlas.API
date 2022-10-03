namespace VRAtlas.Models.Options;

public class CloudflareOptions
{
    public string ApiKey { get; set; } = null!;
    public string AccountId { get; set; } = null!;
    public ImageVariants Variants { get; set; } = new();
}