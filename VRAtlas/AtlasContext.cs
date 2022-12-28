using Microsoft.EntityFrameworkCore;
using VRAtlas.Models;

namespace VRAtlas;

public class AtlasContext : DbContext
{
    public DbSet<Tag> Tags => Set<Tag>();

    public DbSet<User> Users => Set<User>();

    public DbSet<Event> Events => Set<Event>();

    public DbSet<Group> Groups => Set<Group>();
    
    public AtlasContext(DbContextOptions options) : base(options)
    {

    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Tag>().HasMany<Event>().WithMany(t => t.Tags);
    }
}