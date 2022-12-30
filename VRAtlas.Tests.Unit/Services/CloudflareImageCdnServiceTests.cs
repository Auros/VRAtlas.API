using FluentAssertions;
using MockHttpClient;
using NSubstitute;
using System.Net;
using VRAtlas.Logging;
using VRAtlas.Services;
using Xunit;

namespace VRAtlas.Tests.Unit.Services;

public class CloudflareImageCdnServiceTests
{
    private readonly CloudflareImageCdnService _sut;
    private readonly IHttpClientFactory _httpClientFactory = Substitute.For<IHttpClientFactory>();
    private readonly IAtlasLogger<CloudflareImageCdnService> _atlasLogger = Substitute.For<IAtlasLogger<CloudflareImageCdnService>>();

    public CloudflareImageCdnServiceTests()
	{
        _sut = new CloudflareImageCdnService(_atlasLogger, _httpClientFactory);
	}

    [Fact]
    public async Task GetUploadUriAsync_ShouldReturnUri_WhenGivenValidUrl()
    {
        // Arrange
        Guid userId = Guid.NewGuid();
        string uploadURL = "https://myuploadurl.com/path";
        MockHttpClient.MockHttpClient mockClient = new() { BaseAddress = new Uri("https://localhost") };
        _httpClientFactory.CreateClient("Cloudflare").Returns(mockClient);
        mockClient.When("/images/v2/direct_upload").Then(_ => new HttpResponseMessage().WithJsonContent(new
        {
            result = new { uploadURL }
        }));

        // Act
        var uri = await _sut.GetUploadUriAsync(userId);
    
        // Assert
        uri.Should().Be(uploadURL);
        _atlasLogger.Received(1).LogInformation(Arg.Is("Fetching upload url for {Uploader}"), Arg.Is(userId));
        _atlasLogger.Received(1).LogInformation(Arg.Is("Uploading to Cloudflare via Direct Upload"));
        _atlasLogger.Received(1).LogInformation(Arg.Is("Successfully received an upload url for the uploader {Uploader}"), Arg.Is(userId));
    }

    [Fact]
    public async Task GetUploadUriAsync_ShouldThrow_WhenFailed()
    {
        // Arrange
        Guid userId = Guid.NewGuid();
        MockHttpClient.MockHttpClient mockClient = new() { BaseAddress = new Uri("https://localhost") };
        _httpClientFactory.CreateClient("Cloudflare").Returns(mockClient);
        mockClient.When("/images/v2/direct_upload").Then(_ => new HttpResponseMessage(HttpStatusCode.Unauthorized));

        // Act
        Func<Task> act = async () => { await _sut.GetUploadUriAsync(userId); };

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("Could not acquire upload url from Cloudflare");
        _atlasLogger.Received(1).LogInformation(Arg.Is("Fetching upload url for {Uploader}"), Arg.Is(userId));
        _atlasLogger.Received(1).LogInformation(Arg.Is("Uploading to Cloudflare via Direct Upload"));
        _atlasLogger.Received(1).LogCritical(Arg.Is("Failed to acquire upload url from Cloudflare, {StatusCode}"), Arg.Is(HttpStatusCode.Unauthorized));
    }

    [Fact]
    public async Task UploadAsync_ShouldReturnResourceId_WhenGivenValidResourceId()
    {
        // Arrange
        Guid expectedResourceId = Guid.NewGuid();
        Uri source = new("https://my.stuff.com/image.png");
        MockHttpClient.MockHttpClient mockClient = new() { BaseAddress = new Uri("https://localhost") };
        _httpClientFactory.CreateClient("Cloudflare").Returns(mockClient);
        mockClient.When("/images/v1").Then(_ => new HttpResponseMessage().WithJsonContent(new
        {
            result = new { id = expectedResourceId }
        }));

        // Act
        var resourceId = await _sut.UploadAsync(source, null);

        // Assert
        resourceId.Should().Be(expectedResourceId);
        _atlasLogger.Received(1).LogInformation(Arg.Is("Uploading {ImageUrl} to Cloudflare"), Arg.Is(source));
        _atlasLogger.Received(1).LogInformation(Arg.Is("Uploading to Cloudflare via Url Upload"));
        _atlasLogger.Received(1).LogInformation(Arg.Is("Successfully uploaded the image from url {Source} to resource identifier {ResourceId}"), Arg.Is(source), Arg.Is(expectedResourceId));
    }

    [Fact]
    public async Task UploadAsync_ShouldThrow_WhenFailed()
    {
        // Arrange
        Guid expectedResourceId = Guid.NewGuid();
        Uri source = new("https://my.stuff.com/image.png");
        MockHttpClient.MockHttpClient mockClient = new() { BaseAddress = new Uri("https://localhost") };
        _httpClientFactory.CreateClient("Cloudflare").Returns(mockClient);
        mockClient.When("/images/v1").Then(_ => new HttpResponseMessage(HttpStatusCode.BadRequest));

        // Act
        Func<Task> act = async () => { await _sut.UploadAsync(source, null); };

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("Could not upload image from url to Cloudflare");
        _atlasLogger.Received(1).LogInformation(Arg.Is("Uploading {ImageUrl} to Cloudflare"), Arg.Is(source));
        _atlasLogger.Received(1).LogInformation(Arg.Is("Uploading to Cloudflare via Url Upload"));
        _atlasLogger.Received(1).LogCritical(Arg.Is("Failed to upload image {Source} from url, {StatusCode}"), Arg.Is(source), Arg.Is(HttpStatusCode.BadRequest));
    }

    [Fact]
    public async Task ValidateAsync_ShouldBeSuccessful_WhenGivenValidResourceId()
    {
        // Arrange
        Guid? uploaderId = null;
        Guid expectedResourceId = Guid.NewGuid();
        Uri source = new("https://my.stuff.com/image.png");
        MockHttpClient.MockHttpClient mockClient = new() { BaseAddress = new Uri("https://localhost") };
        _httpClientFactory.CreateClient("Cloudflare").Returns(mockClient);
        mockClient.When($"/images/v1/{expectedResourceId}").Then(_ => new HttpResponseMessage().WithJsonContent(new
        {
            result = new { id = expectedResourceId }
        }));

        // Act
        var isValid = await _sut.ValidateAsync(expectedResourceId, null);

        // Assert
        isValid.Should().BeTrue();
        _atlasLogger.Received(1).LogDebug(Arg.Is("Starting image validation for upload {UploadId} and uploader {UploaderId}"), Arg.Is(expectedResourceId), Arg.Is(uploaderId));
        _atlasLogger.Received(1).LogDebug(Arg.Is("Successfuly validated the image {UploadId}"), Arg.Is(expectedResourceId));
    }

    [Fact]
    public async Task ValidateAsync_ShouldBeSuccessful_WhenGivenValidResourceAndUserId()
    {
        // Arrange
        Guid? uploaderId = Guid.NewGuid();
        Guid expectedResourceId = Guid.NewGuid();
        Uri source = new("https://my.stuff.com/image.png");
        MockHttpClient.MockHttpClient mockClient = new() { BaseAddress = new Uri("https://localhost") };
        _httpClientFactory.CreateClient("Cloudflare").Returns(mockClient);
        mockClient.When($"/images/v1/{expectedResourceId}").Then(_ => new HttpResponseMessage().WithJsonContent(new
        {
            result = new { id = expectedResourceId, meta = new { uploaderId } }
        }));

        // Act
        var isValid = await _sut.ValidateAsync(expectedResourceId, uploaderId);

        // Assert
        isValid.Should().BeTrue();
        _atlasLogger.Received(1).LogDebug(Arg.Is("Starting image validation for upload {UploadId} and uploader {UploaderId}"), Arg.Is(expectedResourceId), Arg.Is(uploaderId));
        _atlasLogger.Received(1).LogDebug(Arg.Is("Successfuly validated the image {UploadId}"), Arg.Is(expectedResourceId));
    }
}