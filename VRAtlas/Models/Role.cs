using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace VRAtlas.Models;

[Index(nameof(Name))]
public class Role
{
    [Key]
    public string Name { get; set; } = string.Empty;

    public List<string> Permissions { get; set; } = new();
}