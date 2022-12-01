using FluentAssertions;
using Microsoft.Extensions.Options;
using MockHttpClient;
using NodaTime;
using NSubstitute;
using System.Net;
using System.Text.Json.Serialization;
using VRAtlas.Logging;
using VRAtlas.Options;
using VRAtlas.Services;
using Xunit;

namespace VRAtlas.Tests.Unit.Services;

public class AuthServiceTests
{
    private readonly AuthService _sut;
    private readonly IClock _clock = Substitute.For<IClock>();
    private readonly IHttpClientFactory _httpClientFactory = Substitute.For<IHttpClientFactory>();
    private readonly IOptions<Auth0Options> _auth0Options = Substitute.For<IOptions<Auth0Options>>();
    private readonly IAtlasLogger<AuthService> _atlasLogger = Substitute.For<IAtlasLogger<AuthService>>();

    public AuthServiceTests()
    {
        _sut = new AuthService(_clock, _atlasLogger, _auth0Options, _httpClientFactory);
    }

    [Fact]
    public async Task AssignRolesAsync_ShouldNotThrow_WhenValid()
    {
        // Arrange
        string userId = "myUserId";
        TokenResponse tokenResponse = new()
        {
            AccessToken = "my.access.token",
            ExpiresInSeconds = 86400
        };
        _auth0Options.Value.Returns(new Auth0Options
        {
            Audience = "audience",
            ClientId = "clientid",
            ClientSecret = "clientsecret",
            Domain = "https://localhost"
        });
        MockHttpClient.MockHttpClient mockClient = new()
        {
            BaseAddress = new Uri("https://localhost")
        };
        _httpClientFactory.CreateClient("Auth0").Returns(mockClient);
        mockClient.When($"/api/v2/users/{userId}/roles").Then(_ => new HttpResponseMessage());
        mockClient.When("/oauth/token").Then(_ => new HttpResponseMessage().WithJsonContent(tokenResponse));

        // Act
        await _sut.AssignRolesAsync(userId, new string[] { "MyRole" });

        // Assert
        _atlasLogger.Received(1).LogInformation(Arg.Is("Starting client credential request"));
        _atlasLogger.Received(1).LogInformation(Arg.Is("Successfully performed credential request"));
    }

    [Fact]
    public async Task AssignRolesAsync_ShouldThrow_WhenRoleAssignmentFails()
    {
        // Arrange
        string userId = "myUserId";
        TokenResponse tokenResponse = new()
        {
            AccessToken = "my.access.token",
            ExpiresInSeconds = 86400
        };
        _auth0Options.Value.Returns(new Auth0Options
        {
            Audience = "audience",
            ClientId = "clientid",
            ClientSecret = "clientsecret",
            Domain = "https://localhost"
        });
        MockHttpClient.MockHttpClient mockClient = new()
        {
            BaseAddress = new Uri("https://localhost")
        };
        _httpClientFactory.CreateClient("Auth0").Returns(mockClient);
        mockClient.When($"/api/v2/users/{userId}/roles").Then(_ => new HttpResponseMessage(HttpStatusCode.Unauthorized));
        mockClient.When("/oauth/token").Then(_ => new HttpResponseMessage().WithJsonContent(tokenResponse));

        // Act
        Func<Task> act = async () => { await _sut.AssignRolesAsync(userId, new string[] { "MyRole" }); };

        // Assert
        await act.Should().ThrowAsync<HttpRequestException>();
        _atlasLogger.Received(1).LogInformation(Arg.Is("Starting client credential request"));
        _atlasLogger.Received(1).LogInformation(Arg.Is("Successfully performed credential request"));
    }

    private class TokenResponse
    {
        [JsonPropertyName("access_token")]
        public string AccessToken { get; set; } = null!;

        [JsonPropertyName("expires_in")]
        public int ExpiresInSeconds { get; set; }
    }
}
