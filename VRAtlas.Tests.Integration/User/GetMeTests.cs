using FluentAssertions;
using System.Net.Http.Json;
using System.Net;
using Xunit;
using System.Net.Http.Headers;

namespace VRAtlas.Tests.Integration.User;

[CollectionDefinition(UserCollection.Definition)]
public class GetMeTests : IClassFixture<VRAtlasFactory>
{
    private readonly HttpClient _httpClient;

    public GetMeTests(VRAtlasFactory atlasFactory)
    {
        _httpClient = atlasFactory.CreateClient();
    }

    [Fact]
    public async Task GetMe_ShouldReturnSelf_WithValidValidAuthHeader()
    {
        // Arrange: Setup Variables
        var escape = Uri.EscapeDataString;
        var code = TestConstants.ValidAuth0Code;
        var redirectUri = TestConstants.ValidAuth0RedirectUrl;
        var validAccessToken = TestConstants.ValidUserAccessToken;

        var msg = new HttpRequestMessage
        {
            RequestUri = new Uri("user/@me", UriKind.Relative),
            Method = HttpMethod.Get
        };
        msg.Headers.Authorization = new AuthenticationHeaderValue("Test", validAccessToken);

        // Arrange: Create user
        using var _ = await _httpClient.GetAsync($"auth/token?code={escape(code)}&redirectUri={redirectUri}");

        // Act
        using var response = await _httpClient.SendAsync(msg);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var user = await response.Content.ReadFromJsonAsync<Models.User>();

        user.Should().NotBeNull();
        user!.Username.Should().Be(TestConstants.ValidUserName);
    }

    [Fact]
    public async Task GetMe_ShouldReturnUnauthorized_WithInvalidHeader()
    {
        // Arrange: Setup Variables
        var escape = Uri.EscapeDataString;
        var code = TestConstants.ValidAuth0Code;
        var redirectUri = TestConstants.ValidAuth0RedirectUrl;

        var msg = new HttpRequestMessage
        {
            RequestUri = new Uri("user/@me", UriKind.Relative),
            Method = HttpMethod.Get
        };
        msg.Headers.Authorization = new AuthenticationHeaderValue("Test", "a really not valid auth header");

        // Arrange: Create user
        using var _ = await _httpClient.GetAsync($"auth/token?code={escape(code)}&redirectUri={redirectUri}");

        // Act
        using var response = await _httpClient.SendAsync(msg);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetMe_ShouldReturnUnauthorized_WhenUserDoesNotExist()
    {
        // Arrange
        var msg = new HttpRequestMessage
        {
            RequestUri = new Uri("user/@me", UriKind.Relative),
            Method = HttpMethod.Get
        };
        msg.Headers.Authorization = new AuthenticationHeaderValue("Test", "valid.no-exist");

        // Act
        using var response = await _httpClient.SendAsync(msg);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetMe_ShouldReturnUnauthorized_WithNoAuthHeader()
    {
        // Arrange: Setup Variables
        var escape = Uri.EscapeDataString;
        var code = TestConstants.ValidAuth0Code;
        var redirectUri = TestConstants.ValidAuth0RedirectUrl;

        // Arrange: Create user
        using var _ = await _httpClient.GetAsync($"auth/token?code={escape(code)}&redirectUri={redirectUri}");

        // Act
        using var response = await _httpClient.GetAsync("user/@me");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}