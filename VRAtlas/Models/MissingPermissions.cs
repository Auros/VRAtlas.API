namespace VRAtlas.Models;

public class MissingPermissions
{
    public string Error => "Missing Permissions";

    public IReadOnlyList<string> Permissions { get; }

    public MissingPermissions(IReadOnlyList<string> missingPermissions)
    {
        Permissions = missingPermissions;
    }
}