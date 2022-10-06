namespace VRAtlas.Models.Bodies;

public class UpdateRoleBody
{
    public string Name { get; set; } = string.Empty;

    public List<string> Permissions { get; set; } = new();
}