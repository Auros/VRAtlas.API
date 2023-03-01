using System.Text.Json.Serialization;

namespace VRAtlas.Models.DTO;

public class NotificationInfoDTO
{
    [JsonPropertyName("atStart")]
    public required bool AtStart { get; set; }

    [JsonPropertyName("atThirtyMinutes")]
    public required bool AtThirtyMinutes { get; set; }

    [JsonPropertyName("atOneHour")]
    public required bool AtOneHour { get; set; }

    [JsonPropertyName("atOneDay")]
    public required bool AtOneDay { get; set; }
}
