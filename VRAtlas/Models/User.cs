using Microsoft.EntityFrameworkCore;

namespace VRAtlas.Models;

[Index(nameof(Id))]
public class User
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;
}