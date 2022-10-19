using Microsoft.EntityFrameworkCore;
using VRAtlas.Models;

namespace VRAtlas;

public class AtlasContext : DbContext
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Context> Contexts => Set<Context>();

    public AtlasContext(DbContextOptions options) : base(options)
    {

    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Setup user roles as a many-to-many relationship
        modelBuilder.Entity<Role>().HasMany<User>().WithMany(u => u.Roles);
    }
}