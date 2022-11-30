using Microsoft.Extensions.Options;
using NodaTime;
using System.Text.Json.Serialization;
using VRAtlas.Logging;
using VRAtlas.Options;

namespace VRAtlas.Services;

public interface IAuthService
{
    Task AssignRolesAsync(string userId, IEnumerable<string> roles);
}

public class AuthService : IAuthService
{
    private Instant _timeUntilRefresh;
    private string _accessToken = string.Empty;

    private readonly IClock _clock;
    private readonly IAtlasLogger _atlasLogger;
    private readonly IOptions<Auth0Options> _auth0Options;
    private readonly IHttpClientFactory _httpClientFactory;

    public AuthService(IClock clock, IAtlasLogger<AuthService> atlasLogger, IOptions<Auth0Options> auth0Options, IHttpClientFactory httpClientFactory)
    {
        _clock = clock;
        _atlasLogger = atlasLogger;
        _auth0Options = auth0Options;
        _httpClientFactory = httpClientFactory;
    }

    public async Task AssignRolesAsync(string userId, IEnumerable<string> roles)
    {
        await EnsureAuthTokenExists();

        var client = _httpClientFactory.CreateClient("Auth0");
        var response = await client.PostAsJsonAsync($"api/v2/users/{userId}/roles", new
        {
            roles
        });

        response.EnsureSuccessStatusCode();
    }

    private async Task EnsureAuthTokenExists()
    {
        if (_timeUntilRefresh > _clock.GetCurrentInstant())
            return;

        var options = _auth0Options.Value;
        var client = _httpClientFactory.CreateClient("Auth0");

        _atlasLogger.LogInformation("Starting client credential request");
        var response = await client.PostAsync("oauth/token", new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("grant_type", "client_credentials"),
            new KeyValuePair<string, string>("client_id", options.ClientId),
            new KeyValuePair<string, string>("client_secret", options.ClientSecret),
            new KeyValuePair<string, string>("audience", options.Audience)
        }));
        
        if (!response.IsSuccessStatusCode)
        {
            _atlasLogger.LogCritical("Failed to receive client credential grant from Auth0, {StatusCode}", response.StatusCode);
            throw new InvalidOperationException($"Failed to receive oauth client credential grant from Auth0: {response.StatusCode}");
        }

        _atlasLogger.LogInformation("Successfully performed credential request");
        var tokenResponse = await response.Content.ReadFromJsonAsync<TokenResponse>();
        var time = _clock.GetCurrentInstant() + Duration.FromSeconds(tokenResponse!.ExpiresInSeconds);
        _accessToken = tokenResponse!.AccessToken;
        _timeUntilRefresh = time;
    }

    private class TokenResponse
    {
        [JsonPropertyName("access_token")]
        public required string AccessToken { get; set; }

        [JsonPropertyName("expires_in")]
        public required int ExpiresInSeconds { get; set; }
    }
}