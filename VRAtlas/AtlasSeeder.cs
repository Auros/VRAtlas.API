using Microsoft.EntityFrameworkCore;
using VRAtlas.Models;
using VRAtlas.Services;

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
        var mercuryContext = container.GetRequiredService<AtlasContext>();

        logger.LogInformation("Attempting to perform database migration");
        await mercuryContext.Database.MigrateAsync();
        logger.LogInformation("Disposing migration container");

        // Now, we handle roles and permission setup.
        var userPermissionService = container.GetRequiredService<IUserPermissionService>();
        var administratorRole = await mercuryContext.Roles.FirstOrDefaultAsync(r => r.Name == AtlasConstants.AdministratorRoleName);
        var defaultRole = await mercuryContext.Roles.FirstOrDefaultAsync(r => r.Name == AtlasConstants.DefaultRoleName);
        var defaultPermissions = app.Configuration.GetSection("DefaultPermissions")?.Get<string[]>() ?? Array.Empty<string>();

        if (administratorRole is null)
        {
            administratorRole = new Role
            {
                Name = AtlasConstants.AdministratorRoleName,
                Permissions = new List<string> { AtlasConstants.SpecialAdministrator }
            };
            mercuryContext.Roles.Add(administratorRole);
        }
        if (defaultRole is null)
        {
            defaultRole = new Role
            {
                Name = AtlasConstants.DefaultRoleName
            };
            mercuryContext.Roles.Add(defaultRole);
        }
        defaultRole.Permissions = defaultPermissions.ToList();

        // Wipe everything from the permission cache if necessary.
        await userPermissionService.ClearAll();

        // Save the database changes
        await mercuryContext.SaveChangesAsync();
    }
}