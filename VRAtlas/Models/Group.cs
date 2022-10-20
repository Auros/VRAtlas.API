using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace VRAtlas.Models;

public class Group
{
    public Guid Id { get; set; }
    
    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<GroupUser> Users { get; set; } = null!;

    [Column(TypeName = "jsonb")]
    public ImageVariants Icon { get; set; } = null!;

    [Column(TypeName = "jsonb")]
    public ImageVariants Banner { get; set; } = null!;
}