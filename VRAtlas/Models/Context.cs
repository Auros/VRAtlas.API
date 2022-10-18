using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;

namespace VRAtlas.Models;

[Index(nameof(Id))]
[Index(nameof(Name))]
public class Context
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public ContextType Type { get; set; }

    [Column(TypeName = "jsonb")]
    public ImageVariants Icon { get; set; } = null!;
}