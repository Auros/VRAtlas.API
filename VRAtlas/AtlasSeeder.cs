using Microsoft.EntityFrameworkCore;

namespace VRAtlas;

public static class AtlasSeeder
{
    public static async Task SeedAtlas(this WebApplication app)
    {
        // Setup a scope to start seeding.
        await using var scope = app.Services.CreateAsyncScope();
        var container = scope.ServiceProvider;

        // First, run any necessary migrations to the database.
        var logger = container.GetRequiredService<ILogger<Program>>();
        var atlasContext = container.GetRequiredService<AtlasContext>();

        logger.LogInformation("Attempting to perform database migration");
        await atlasContext.Database.MigrateAsync();
        logger.LogInformation("Disposing migration container");

        // Save the database changes
        await atlasContext.SaveChangesAsync();
    }
}