using System.ComponentModel.DataAnnotations.Schema;

namespace VRAtlas.Models;

public class Group
{
    public Guid Id { get; set; }
    
    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public List<GroupUser> Users { get; set; } = new();

    [Column(TypeName = "jsonb")]
    public ImageVariants Icon { get; set; } = null!;

    [Column(TypeName = "jsonb")]
    public ImageVariants Banner { get; set; } = null!;
}