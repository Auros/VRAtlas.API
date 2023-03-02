using VRAtlas.Attributes;

namespace VRAtlas.Models;

[VisualName("API Status")]
public class ApiStatus
{
    public required string Status { get; init; }
}