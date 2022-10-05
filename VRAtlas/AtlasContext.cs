using Microsoft.EntityFrameworkCore;
using VRAtlas.Models;

namespace VRAtlas;

public class AtlasContext : DbContext
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();

    public AtlasContext(DbContextOptions options) : base(options)
    {

    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Role>().HasMany<User>().WithMany(u => u.Roles);
    }
}