using Azure.Storage.Blobs;
using Microsoft.Extensions.Options;
using VRAtlas.Models;
using VRAtlas.Models.Options;

namespace VRAtlas.Services.Implementations;

public class AzureAvatarCdnService : IAvatarCdnService
{
    private readonly ILogger _logger;
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly IOptions<AzureOptions> _azureOptions;

    public AzureAvatarCdnService(ILogger<AzureAvatarCdnService> logger, HttpClient httpClient, IConfiguration configuration, IOptions<AzureOptions> azureOptions)
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
            var blob = await uploadContainer.UploadBlobAsync(fileName, stream);
        }
        catch
        {

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