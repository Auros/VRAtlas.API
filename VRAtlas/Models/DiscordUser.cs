using System.Text.Json.Serialization;

namespace VRAtlas.Models;

public record DiscordUser
{
    public string Id { get; set; } = null!;

    public string Username { get; set; } = null!;

    public string Discriminator { get; set; } = null!;

    public string Avatar { get; set; } = null!;

    public string? Email { get; set; }

    [JsonPropertyName("avatarURL")]
    public string ProfileURL => "https://cdn.discordapp.com/avatars/" + Id + "/" + Avatar + (Avatar[..2] == "a_" ? ".gif" : ".png");
    public string FormattedName() => $"{Username}#{Discriminator}";
}