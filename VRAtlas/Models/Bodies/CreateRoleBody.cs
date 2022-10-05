namespace VRAtlas.Models.Bodies;

public class CreateRoleBody
{
    public string Name { get; set; } = string.Empty;

    public string[] Permissions { get; set; } = Array.Empty<string>();
}