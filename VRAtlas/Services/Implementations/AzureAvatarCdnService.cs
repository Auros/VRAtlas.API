using Microsoft.Extensions.Options;
using VRAtlas.Models.Options;

namespace VRAtlas.Services.Implementations;

public class AzureAvatarCdnService : AzureVariantCdnService, IAvatarCdnService
{
    public AzureAvatarCdnService(ILogger<AzureAvatarCdnService> logger, HttpClient httpClient, IConfiguration configuration, IOptions<AzureOptions> azureOptions) : base(logger, httpClient, configuration, azureOptions)
    {
    }

    protected override string UploadContainer => _azureOptions.Value.AvatarUploadContainer;

    protected override string OutputContainer => _azureOptions.Value.AvatarOutputContainer;
}