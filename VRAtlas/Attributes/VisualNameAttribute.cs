namespace VRAtlas.Attributes;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum)]
public class VisualNameAttribute : Attribute
{
    public string Name { get; init; }

	public VisualNameAttribute(string name) => Name = name;
}