using VRAtlas.Attributes;

namespace VRAtlas.Models;

[VisualName("Event Status")]
public enum EventStatus
{
    Unlisted,
    Announced,
    Preliminary,
    Started,
    Concluded,
    Canceled
}