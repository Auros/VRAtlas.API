using VRAtlas.Models.Crossposters;

namespace VRAtlas.Options;

public class CrosspostingOptions
{
    public const string Name = "Crossposting";

    public int SynchronizationIntervalInMinutes { get; set; } = 10;

    public VRCC? VRCC { get; set; }
}