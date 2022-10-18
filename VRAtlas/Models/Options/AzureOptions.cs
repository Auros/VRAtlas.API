namespace VRAtlas.Models.Options;

public class AzureOptions
{
    public string AvatarOutputContainer { get; set; } = "avatars";
    public string AvatarUploadContainer { get; set; } = string.Empty;
    public string CdnEndpointName { get; set; } = string.Empty;
    public ImageVariants Variants { get; set; } = new();
}