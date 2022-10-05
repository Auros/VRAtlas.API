using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace VRAtlas.Models;

[Index(nameof(Name))]
public class Role : IEquatable<Role>
{
    [Key]
    public string Name { get; set; } = string.Empty;

    public List<string> Permissions { get; set; } = new();

    public bool Equals(Role? other) => other?.Name == Name;

    public override bool Equals(object? obj) => Equals(obj as Role);

    public override int GetHashCode() => Name.GetHashCode();
}