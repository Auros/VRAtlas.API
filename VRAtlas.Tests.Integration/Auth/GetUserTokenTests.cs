using FluentAssertions;
using System.Net;
using System.Net.Http.Json;
using VRAtlas.Models;
using Xunit;

namespace VRAtlas.Tests.Integration.Auth;

[CollectionDefinition(AuthCollection.Definition)]
public class GetUserTokenTests : IClassFixture<VRAtlasFactory>
{
    private readonly HttpClient _httpClient;

    public GetUserTokenTests(VRAtlasFactory atlasFactory)
    {
        _httpClient = atlasFactory.CreateClient();
    }

    [Fact]
    public async Task GetAuthToken_ShouldReturnValidTokens_WithValidInputs()
    {
        // Arrange
        var escape = Uri.EscapeDataString;
        var code = TestConstants.ValidAuth0Code;
        var redirectUri = TestConstants.ValidAuth0RedirectUrl;
        UserTokens expectedTokens = new()
        {
            IdToken = TestConstants.ValidUserIdToken,
            AccessToken = TestConstants.ValidUserAccessToken,
            ExpiresIn = TestConstants.ValidUserTokenExpiration,
            RefreshToken = null!,
        };

        // Act
        using var response = await _httpClient.GetAsync($"auth/token?code={escape(code)}&redirectUri={redirectUri}");
        var status = await response.Content.ReadFromJsonAsync<UserTokens>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        status.Should().BeEquivalentTo(expectedTokens);
    }

    [Fact]
    public async Task GetAuthToken_ShouldReturnInternalError_WithInvalidCode()
    {
        // Arrange
        var escape = Uri.EscapeDataString;
        var code = "an-invalid-code";
        var redirectUri = TestConstants.ValidAuth0RedirectUrl; //"https://invalid-uri.vratlas.io";

        // Act
        using var response = await _httpClient.GetAsync($"auth/token?code={escape(code)}&redirectUri={redirectUri}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task GetAuthToken_ShouldReturnInternalError_WithInvalidRedirectUri()
    {
        // Arrange
        var escape = Uri.EscapeDataString;
        var code = TestConstants.ValidAuth0Code;
        var redirectUri = "https://invalid-uri.vratlas.io";

        // Act
        using var response = await _httpClient.GetAsync($"auth/token?code={escape(code)}&redirectUri={redirectUri}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
    }
}