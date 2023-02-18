using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System.Text.Json;
using VRAtlas.Models;

namespace VRAtlas;

public class AtlasContext : DbContext
{
    public DbSet<Tag> Tags => Set<Tag>();

    public DbSet<User> Users => Set<User>();

    public DbSet<Event> Events => Set<Event>();

    public DbSet<Group> Groups => Set<Group>();
    
    public DbSet<Follow> Follows => Set<Follow>();

    public DbSet<EventTag> EventTags => Set<EventTag>();

    public DbSet<GroupMember> GroupMembers => Set<GroupMember>();

    public DbSet<Notification> Notifications => Set<Notification>();

    public AtlasContext(DbContextOptions options) : base(options)
    {

    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder
            .Entity<Group>()
            .HasMany(g => g.Members)
            .WithOne(m => m.Group)
            .OnDelete(DeleteBehavior.Cascade);
        
        // Register value converter for user's Links property.
        // We want this DbContext to be fundamentally database agonistic,
        // so we're avoiding using database specific types like PostgreSQL's jsonb
        modelBuilder
            .Entity<User>()
            .Property(u => u.Links)
            .HasConversion(l => SerializeList(l), l => DeserializeList<string>(l));

        // Register value comparer for user's Links property. This cannot
        // be chained with the conversion registration call above.
        modelBuilder
            .Entity<User>()
            .Property(u => u.Links)
            .Metadata.SetValueComparer(GenerateListValueComparer<string>());
    }

    private static string SerializeList<T>(List<T> value) => JsonSerializer.Serialize(value);

    private static List<T> DeserializeList<T>(string value) => string.IsNullOrWhiteSpace(value) ? new List<T>() : JsonSerializer.Deserialize<List<T>>(value)!;

    private static ValueComparer<List<T>> GenerateListValueComparer<T>() => new(
        (c1, c2) => c1!.SequenceEqual(c2!),
        c => c.Aggregate(
            0,
            (a, v) => HashCode.Combine(a, v!.GetHashCode())
        ),
        c => c.ToList()
    );

}