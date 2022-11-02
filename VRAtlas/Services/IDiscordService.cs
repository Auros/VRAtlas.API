using VRAtlas.Models;

namespace VRAtlas.Services;

public interface IDiscordService
{
    Task<string?> GetAccessTokenAsync(string code);
    Task<DiscordUser?> GetProfileAsync(string accessToken);
}
