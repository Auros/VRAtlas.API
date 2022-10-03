using Microsoft.Extensions.Options;
using System.Text.Json;
using VRAtlas.Models.Options;
using VRAtlas.Models;

namespace VRAtlas.Services.Implementations;

public class CloudflareImageCdnService : IImageCdnService
{
    private readonly ILogger _logger;
    private readonly HttpClient _httpClient;
    private readonly IOptions<CloudflareOptions> _cloudflareOptions;
    
    public CloudflareImageCdnService(ILogger<CloudflareImageCdnService> logger, HttpClient httpClient, IOptions<CloudflareOptions> cloudflareOptions)
    {
        _logger = logger;
        _httpClient = httpClient;
        _cloudflareOptions = cloudflareOptions;
    }

    public async Task<ImageVariants?> UploadAsync(string url, string? metadata = null)
    {
        // Documentation for this upload step:
        // https://developers.cloudflare.com/images/cloudflare-images/upload-images/upload-via-url/

        var options = _cloudflareOptions.Value;
        var cloudflareUrl = new Uri($"https://api.cloudflare.com/client/v4/accounts/{options.AccountId}/images/v1");
        _logger.LogDebug("Uploading {ImageUrl} to Cloudflare via {UploadUrl}", url, cloudflareUrl);

        _logger.LogDebug("Creating content body");
        MultipartFormDataContent content = new();
        if (metadata is not null)
            content.Add(new StringContent(metadata), "\"metadata\"");
        content.Add(new StringContent(url), "\"url\"");

        _logger.LogDebug("Creating request body");
        HttpRequestMessage msg = new()
        {
            RequestUri = new Uri($"https://api.cloudflare.com/client/v4/accounts/{options.AccountId}/images/v1"),
            Method = HttpMethod.Post,
            Content = content,
        };
        msg.Headers.Add("Authorization", $"Bearer {options.ApiKey}");

        _logger.LogDebug("Sending request body");
        using var response = await _httpClient.SendAsync(msg);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Unable to upload the image {ImageUrl} to {UploadUrl}, Status Code = {StatusCode}", url, cloudflareUrl, response.StatusCode);
            return null;
        }    
        
        var body = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(body);

        // Get the result variants
        var variants = doc.RootElement
            .GetProperty("result")
            .GetProperty("variants")
            .Deserialize<string[]>()!
        ;

        // Search for the variants in response that match our config.
        return new ImageVariants
        {
            Full = variants.FirstOrDefault(v => v.EndsWith(options.Variants.Full))!,
            Large = variants.FirstOrDefault(v => v.EndsWith(options.Variants.Large))!,
            Medium = variants.FirstOrDefault(v => v.EndsWith(options.Variants.Medium))!,
            Small = variants.FirstOrDefault(v => v.EndsWith(options.Variants.Small))!,
            Mini = variants.FirstOrDefault(v => v.EndsWith(options.Variants.Mini))!,
        };
    }
}