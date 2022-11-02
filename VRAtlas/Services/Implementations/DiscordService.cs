using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Text.Json;
using VRAtlas.Models;
using VRAtlas.Models.Options;

namespace VRAtlas.Services.Implementations;

public class DiscordService : IDiscordService
{
    private readonly ILogger _logger;
    private readonly HttpClient _client;
    private readonly IOptions<DiscordOptions> _discordOptions;

    public DiscordService(ILogger<DiscordService> logger, HttpClient client, IOptions<DiscordOptions> discordSettings)
    {
        _logger = logger;
        _client = client;
        _discordOptions = discordSettings;
    }

    public async Task<string?> GetAccessTokenAsync(string code)
    {
        var discord = _discordOptions.Value;

        _logger.LogDebug("Fetching Access Token");
        Dictionary<string, string> parameters = new()
        {
            { "client_id", discord.ClientId },
            { "client_secret", discord.ClientSecret },
            { "grant_type", "authorization_code" },
            { "code", code },
            { "redirect_uri", discord.RedirectUrl }
        };
        FormUrlEncodedContent content = new(parameters!);
        HttpResponseMessage response = await _client.PostAsync(AtlasConstants.DiscordApiUrl + "/oauth2/token", content);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Could not get access token. {ReasonPhrase}", response.ReasonPhrase);
            return null;
        }
        _logger.LogDebug("Received Access Token");
        var body = await response.Content.ReadAsStringAsync();
        return JsonDocument.Parse(body).RootElement.GetProperty("access_token").GetString();
    }

    public async Task<DiscordUser?> GetProfileAsync(string accessToken)
    {
        _logger.LogDebug("Getting active user profile.");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        HttpResponseMessage response = await _client.GetAsync(AtlasConstants.DiscordApiUrl + "/users/@me");
        if (response.IsSuccessStatusCode)
        {
            string responseString = await response.Content.ReadAsStringAsync();
            DiscordUser? discordUser = JsonSerializer.Deserialize<DiscordUser>(responseString, new JsonSerializerOptions(JsonSerializerDefaults.Web));
            _logger.LogDebug("User Profile {Username}#{Discriminator} Found", discordUser?.Username, discordUser?.Discriminator);
            return discordUser;
        }
        _logger.LogWarning("Could not get user profile. {ReasonPhrase}", response.ReasonPhrase);
        return null;
    }
}