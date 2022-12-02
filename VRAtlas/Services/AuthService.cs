using Microsoft.Extensions.Options;
using NodaTime;
using System.Net.Http.Headers;
using System.Text.Json.Serialization;
using VRAtlas.Logging;
using VRAtlas.Models;
using VRAtlas.Options;

namespace VRAtlas.Services;

public interface IAuthService
{
    Task AssignRolesAsync(string userId, IEnumerable<string> roles);
    Task<UserTokens?> GetUserTokensAsync(string code, string redirectUri); 
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
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);
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
            new KeyValuePair<string, string>("audience", options.Audience),
            new KeyValuePair<string, string>("client_id", options.ClientId),
            new KeyValuePair<string, string>("grant_type", "client_credentials"),
            new KeyValuePair<string, string>("client_secret", options.ClientSecret),
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

    public async Task<UserTokens?> GetUserTokensAsync(string code, string redirectUri)
    {
        var options = _auth0Options.Value;
        var client = _httpClientFactory.CreateClient("Auth0");

        _atlasLogger.LogInformation("Requesting token details with code from Auth0");

        var response = await client.PostAsync("oauth/token", new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("code", code),
            new KeyValuePair<string, string>("redirect_uri", redirectUri),
            new KeyValuePair<string, string>("client_id", options.ClientId),
            new KeyValuePair<string, string>("grant_type", "authorization_code"),
            new KeyValuePair<string, string>("client_secret", options.ClientSecret),
        }));

        if (!response.IsSuccessStatusCode)
        {
            _atlasLogger.LogWarning("Unable to acquire user tokens from Auth0 code");
            return null;
        }

        _atlasLogger.LogInformation("Successfully fetched token details with code from Auth0");

        var tokenResponse = await response.Content.ReadFromJsonAsync<TokenResponse>();
        return new UserTokens
        {
            IdToken = tokenResponse!.IdToken!,
            AccessToken = tokenResponse!.AccessToken,
            RefreshToken = tokenResponse!.RefreshToken!,
            ExpiresIn = tokenResponse!.ExpiresInSeconds,
        };
    }

    private class TokenResponse
    {
        [JsonPropertyName("access_token")]
        public string AccessToken { get; set; } = null!;

        [JsonPropertyName("expires_in")]
        public int ExpiresInSeconds { get; set; }

        [JsonPropertyName("id_token")]
        public string? IdToken { get; set; }

        [JsonPropertyName("refresh_token")]
        public string? RefreshToken { get; set; }
    }
}