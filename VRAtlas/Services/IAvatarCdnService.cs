using VRAtlas.Models;

namespace VRAtlas.Services;

public interface IAvatarCdnService
{
    /// <summary>
    /// Uploads an image to a CDN to generate variants
    /// </summary>
    /// <param name="url">The image to download.</param>
    /// <param name="metadata">Metadata to include with the image.</param>
    /// <returns></returns>
    Task<ImageVariants?> UploadAsync(string url, string? metadata = null);

    /// <summary>
    /// Uploads an image to a CDN to generate variants
    /// </summary>
    /// <param name="stream">The stream that contains the image.</param>
    /// <param name="metadata">Metadata to include with the image.</param>
    /// <returns></returns>
    Task<ImageVariants?> UploadAsync(string fileName, Stream stream, string? metadata = null);
}