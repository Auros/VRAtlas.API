using VRAtlas.Models;

namespace VRAtlas.Services;

public interface IImageCdnService
{
    /// <summary>
    /// Uploads an image to a CDN to generate variants
    /// </summary>
    /// <param name="url">The image to download.</param>
    /// <param name="metadata">Metadata to include with the image.</param>
    /// <returns></returns>
    Task<ImageVariants?> UploadAsync(string url, string? metadata = null);
}