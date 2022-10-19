using VRAtlas.Models;

namespace VRAtlas.Services;

public interface IVariantCdnService
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

    /// <summary>
    /// Verifies that an image with the provided id exists and returns its variants.
    /// </summary>
    /// <param name="uploadId">The id of the upload image.</param>
    /// <param name="uploaderId">The id of the uploader. This is to ensure that the person who uploaded it is the only one who can access it.</param>
    /// <returns></returns>
    Task<ImageVariants?> ValidateAsync(string uploadId, string? uploaderId = null);

    /// <summary>
    /// Gets a url to upload an image to.
    /// </summary>
    /// <returns></returns>
    Task<string?> GetUploadUrl(string? uploaderId = null);
}