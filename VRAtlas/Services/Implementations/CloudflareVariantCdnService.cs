using Microsoft.Extensions.Options;
using System.Text.Json;
using VRAtlas.Models.Options;
using VRAtlas.Models;
using System;

namespace VRAtlas.Services.Implementations;

public class CloudflareVariantCdnService : IVariantCdnService
{
    private readonly ILogger _logger;
    private readonly HttpClient _httpClient;
    private readonly IOptions<CloudflareOptions> _cloudflareOptions;
    
    public CloudflareVariantCdnService(ILogger<CloudflareVariantCdnService> logger, HttpClient httpClient, IOptions<CloudflareOptions> cloudflareOptions)
    {
        _logger = logger;
        _httpClient = httpClient;
        _cloudflareOptions = cloudflareOptions;
    }

    public async Task<string?> GetUploadUrl(string? uploaderId = null)
    {
        var options = _cloudflareOptions.Value;
        var cloudflareUrl = new Uri($"https://api.cloudflare.com/client/v4/accounts/{options.AccountId}/images/v2/direct_upload");

        _logger.LogDebug("Creating upload request body");
        MultipartFormDataContent content = new();
        if (uploaderId is not null)
            content.Add(new StringContent("{\"uploaderId\":\"" + uploaderId + "\"}"), "\"metadata\"");

        HttpRequestMessage msg = new()
        {
            RequestUri = cloudflareUrl,
            Method = HttpMethod.Post,
            Content = content,
        };
        msg.Headers.Add("Authorization", $"Bearer {options.ApiKey}");

        using var response = await _httpClient.SendAsync(msg);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Could not create an upload url");
            return null!;
        }

        var body = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(body);

        return doc.RootElement
            .GetProperty("result")
            .GetProperty("uploadURL")
            .Deserialize<string>()
        ;
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
            RequestUri = cloudflareUrl,
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

        return MapVariants(variants, options);
    }

    public Task<ImageVariants?> UploadAsync(string fileName, Stream stream, string? metadata = null)
    {
        throw new NotImplementedException();
    }

    public async Task<ImageVariants?> ValidateAsync(string uploadId, string? uploaderId = null)
    {
        var options = _cloudflareOptions.Value;
        var cloudflareUrl = new Uri($"https://api.cloudflare.com/client/v4/accounts/{options.AccountId}/images/v1/{uploadId}");

        _logger.LogDebug("Creating validation request body");
        HttpRequestMessage msg = new()
        {
            RequestUri = cloudflareUrl,
            Method = HttpMethod.Get
        };
        msg.Headers.Add("Authorization", $"Bearer {options.ApiKey}");

        using var response = await _httpClient.SendAsync(msg);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Image {UploadId} was not uploaded", uploadId);
            return null;
        }

        var body = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(body);

        if (uploaderId is not null)
        {
            var hasMeta = doc.RootElement
                .GetProperty("result")
                .TryGetProperty("meta", out var meta)
            ;

            if (!hasMeta)
                return null;

            var trueUploaderPresent = meta.TryGetProperty("uploaderId", out var uploaderIdProperty);
            if (!trueUploaderPresent || uploaderIdProperty!.GetString() != uploaderId)
                return null;
        }

        // Get the result variants
        var variants = doc.RootElement
            .GetProperty("result")
            .GetProperty("variants")
            .Deserialize<string[]>()!
        ;

        return MapVariants(variants, options);
    }

    private static ImageVariants MapVariants(string[] variants, CloudflareOptions options)
    {
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