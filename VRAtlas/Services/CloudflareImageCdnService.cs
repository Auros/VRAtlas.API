using System.Text.Json.Serialization;
using VRAtlas.Logging;

namespace VRAtlas.Services;

public interface IImageCdnService
{
    /// <summary>
    /// Upload an image from an already existing place on the web.
    /// </summary>
    /// <param name="source">The source of the image.</param>
    /// <param name="metadata">Metadata to include with the uploaded image.</param>
    /// <returns>The resource identifier for the uploaded image.</returns>
    Task<Guid> UploadAsync(Uri source, string? metadata);

    /// <summary>
    /// Gets a URL to send upload data to.
    /// </summary>
    /// <param name="uploaderId">The id of the user who is allowed to upload an image with the resulting uri.</param>
    /// <returns></returns>
    Task<Uri> GetUploadUriAsync(Guid? uploaderId = null);
}

public class CloudflareImageCdnService : IImageCdnService
{
    private readonly IAtlasLogger _atlasLogger;
    private readonly IHttpClientFactory _httpClientFactory;

    public CloudflareImageCdnService(IAtlasLogger atlasLogger, IHttpClientFactory httpClientFactory)
    {
        _atlasLogger = atlasLogger;
        _httpClientFactory = httpClientFactory;
    }

    public async Task<Uri> GetUploadUriAsync(Guid? uploaderId = null)
    {
        var client = _httpClientFactory.CreateClient("Cloudflare");
        _atlasLogger.LogInformation("Fetching upload url for {Uploader}", uploaderId);

        // Setup the http content required for the upload.
        // This includes tagging the uploader id if it's necessary for resource upload validation.
        MultipartFormDataContent content = new();
        if (uploaderId.HasValue)
            content.Add(new StringContent($$"""{"uploaderId":"{{uploaderId.Value}}"}"""), "\"metadata\"");

        // Send the request.
        _atlasLogger.LogInformation("Uploading to Cloudflare via Direct Upload");
        var response = await client.PostAsync("images/v2/direct_upload", content);

        // Ensure the request passes.
        if (!response.IsSuccessStatusCode)
        {
            _atlasLogger.LogCritical("Failed to acquire upload url from Cloudflare, {StatusCode}", response.StatusCode);
            throw new InvalidOperationException("Could not acquire upload url from Cloudflare");
        }

        // Read the response JSON for the upload url.
        _atlasLogger.LogInformation("Successfully received an upload url for the uploader {Uploader}", uploaderId);
        var body = await response.Content.ReadFromJsonAsync<CloudflareResult<UploadUrlResult>>();
        
        return body!.Result.UploadUrl;
    }

    public async Task<Guid> UploadAsync(Uri source, string? metadata)
    {
        var client = _httpClientFactory.CreateClient("Cloudflare");
        _atlasLogger.LogInformation("Uploading {ImageUrl} to Cloudflare", source); 

        // Setup the http content required for the upload.
        // This is where we provide our source url.
        MultipartFormDataContent content = new();
        if (metadata is not null)
            content.Add(new StringContent(metadata), "\"metadata\"");
        content.Add(new StringContent(source.ToString()), "\"url\"");

        // Send the request.
        _atlasLogger.LogInformation("Uploading to Cloudflare via Url Upload");
        var response = await client.PostAsync("images/v1", content);
        
        // Ensure the request passes.
        if (!response.IsSuccessStatusCode)
        {
            _atlasLogger.LogCritical("Failed to upload image {Source} from url, {StatusCode}", source, response.StatusCode);
            throw new InvalidOperationException("Could not upload image from url to Cloudflare");
        }

        // Read the response JSON for the image id. This id can then be used to stitch together
        // the image url from the hash Cloudflare account hash, as well as any variants.
        var body = await response.Content.ReadFromJsonAsync<CloudflareResult<UploadSourceResult>>();
        var id = body!.Result.Id;

        _atlasLogger.LogInformation("Successfully uploaded the image from url {Source} to resource identifier {ResourceId}", source, id);

        return id;
    }

    private class UploadUrlResult
    {
        [JsonPropertyName("uploadURL")]
        public Uri UploadUrl { get; set; } = null!;
    }

    private class UploadSourceResult
    {
        [JsonPropertyName("id")]
        public Guid Id { get; set; }
    }

    private class CloudflareResult<T> where T : class
    {
        [JsonPropertyName("result")]
        public T Result { get; set; } = null!;
    }
}