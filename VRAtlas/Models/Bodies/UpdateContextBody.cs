namespace VRAtlas.Models.Bodies;

public class UpdateContextBody
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public ContextType Type { get; set; }
}