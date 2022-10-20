using Microsoft.EntityFrameworkCore;
using VRAtlas.Models;

namespace VRAtlas;

public class AtlasContext : DbContext
{
    public DbSet<User> Users => Set<User>();

    public DbSet<Role> Roles => Set<Role>();

    public DbSet<Event> Events => Set<Event>();

    public DbSet<Group> Groups => Set<Group>();

    public DbSet<Context> Contexts => Set<Context>();

    public AtlasContext(DbContextOptions options) : base(options)
    {

    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Setup database objects as a many-to-many relationship
        modelBuilder.Entity<Role>().HasMany<User>().WithMany(u => u.Roles);
        modelBuilder.Entity<EventStar>().HasMany<Event>().WithMany(e => e.Stars);
        modelBuilder.Entity<Context>().HasMany<Event>().WithMany(e => e.Contexts);
    }
}