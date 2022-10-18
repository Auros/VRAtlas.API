using Azure.Storage.Blobs;
using Azure;
using Microsoft.Extensions.Options;
using VRAtlas.Models;
using VRAtlas.Models.Options;
using VRAtlas.Services.Implementations;

namespace VRAtlas.Services;

public abstract class AzureVariantCdnService : IVariantCdnService
{
    private readonly ILogger _logger;
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    protected readonly IOptions<AzureOptions> _azureOptions;

    protected abstract string UploadContainer { get; }
    protected abstract string OutputContainer { get; }

    public AzureVariantCdnService(ILogger<AzureAvatarCdnService> logger, HttpClient httpClient, IConfiguration configuration, IOptions<AzureOptions> azureOptions)
    {
        _logger = logger;
        _httpClient = httpClient;
        _configuration = configuration;
        _azureOptions = azureOptions;
    }

    public async Task<ImageVariants?> UploadAsync(string url, string? metadata = null)
    {
        var fileName = Path.GetFileName(url);
        using Stream stream = await _httpClient.GetStreamAsync(url);
        return await UploadAsync(fileName, stream, metadata);
    }

    public async Task<ImageVariants?> UploadAsync(string fileName, Stream stream, string? metadata = null)
    {
        var options = _azureOptions.Value;
        var connString = _configuration.GetConnectionString(nameof(Azure));
        BlobServiceClient client = new(connString);
        var uploadContainer = client.GetBlobContainerClient(options.AvatarUploadContainer);

        try
        {
            // Upload the blob to azure.
            // We have a function which automatically generates image variants on the edge.
            var blob = await uploadContainer.UploadBlobAsync(fileName, stream);
        }
        catch (RequestFailedException rfe)
        {
            _logger.LogWarning(rfe, "An error occured while upload a blob");
            return null;
        }

        var fileNameNoExtension = Path.GetFileNameWithoutExtension(fileName);
        var fileExtension = Path.GetExtension(fileName);

        // Search for the variants in response that match our config.
        return new ImageVariants
        {
            Full = BuildUploadVariantInfo(options.CdnEndpointName, options.AvatarOutputContainer, fileNameNoExtension, fileExtension, options.Variants.Full),
            Large = BuildUploadVariantInfo(options.CdnEndpointName, options.AvatarOutputContainer, fileNameNoExtension, fileExtension, options.Variants.Large),
            Medium = BuildUploadVariantInfo(options.CdnEndpointName, options.AvatarOutputContainer, fileNameNoExtension, fileExtension, options.Variants.Medium),
            Small = BuildUploadVariantInfo(options.CdnEndpointName, options.AvatarOutputContainer, fileNameNoExtension, fileExtension, options.Variants.Small),
            Mini = BuildUploadVariantInfo(options.CdnEndpointName, options.AvatarOutputContainer, fileNameNoExtension, fileExtension, options.Variants.Mini),
        };
    }

    private static string BuildUploadVariantInfo(string endpoint, string container, string fileName, string fileExtension, string variant)
    {
        return $"https://{endpoint}.azureedge.net/{container}/{fileName}_{variant}{fileExtension}";
    }
}