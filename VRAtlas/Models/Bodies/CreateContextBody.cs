namespace VRAtlas.Models.Bodies;

public class CreateContextBody
{
    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public ContextType Type { get; set; }

    public string IconImageId { get; set; } = string.Empty;
}