namespace VRAtlas.Models;

public class MissingPermissions
{
    public string Error { get; } = "Missing Permissions";

    public IReadOnlyList<string> Permissions { get; }

    public MissingPermissions(IReadOnlyList<string> missingPermissions)
    {
        Permissions = missingPermissions;
    }
}