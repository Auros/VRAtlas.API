using System.Text.Json.Serialization;
using VRAtlas.Attributes;

namespace VRAtlas.Models;

[VisualName("User Tokens")]
public class UserTokens
{
    [JsonPropertyName("idToken")]
    public string IdToken { get; set; } = string.Empty;

    [JsonPropertyName("accessToken")]
    public string AccessToken { get; set; } = string.Empty;

    [JsonPropertyName("refreshToken")]
    public string RefreshToken { get; set; } = string.Empty;

    /// <summary>
    /// In Seconds
    /// </summary>
    [JsonPropertyName("expiresIn")]
    public int ExpiresIn { get; set; }
}