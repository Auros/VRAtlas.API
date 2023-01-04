using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace VRAtlas.Tests.Unit;

public sealed class AtlasFixture : IDisposable
{
    private readonly SqliteConnection _sqliteConnection;

    public AtlasContext Context { get; }

    public AtlasFixture()
    {
        _sqliteConnection = new SqliteConnection("Data Source=:memory:");
        _sqliteConnection.Open();

        var builder = new DbContextOptionsBuilder<AtlasContext>(new DbContextOptions<AtlasContext>(new Dictionary<Type, IDbContextOptionsExtension>()));
        builder.UseSqlite(_sqliteConnection, options => options.UseNodaTime());
        AtlasContext atlasContext = new(builder.Options);
        atlasContext.Database.EnsureCreated();

        Context = atlasContext;
    }

    public void Dispose()
    {
        Context.Dispose();
        _sqliteConnection.Close();
    }
}