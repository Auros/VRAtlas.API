using System.Text.Json.Serialization;

namespace VRAtlas.Models;

public class Error
{
    [JsonPropertyName("error")]
    public string ErrorName { get; set; } = string.Empty;
}