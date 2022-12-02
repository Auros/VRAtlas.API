using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;

namespace VRAtlas.Models;

[Index(nameof(Id))]
[Index(nameof(SocialId))]
public class User
{
    public required Guid Id { get; set; }

    public required string Username { get; set; }

    [JsonIgnore]
    public required string SocialId { get; set; }
}