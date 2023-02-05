using System.ComponentModel;

namespace VRAtlas.Models;

[DisplayName("API Status")]
public class ApiStatus
{
    public required string Status { get; init; }
}