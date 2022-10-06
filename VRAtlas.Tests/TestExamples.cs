using System.Net.Http.Headers;

namespace VRAtlas.Tests;

internal static class TestExamples
{
    private const string ExampleImageUrl = "https://avatars.githubusercontent.com/u/41306347";

    public static ImageVariants ExampleImageVariants = new()
    {
        Full = ExampleImageUrl,
        Large = ExampleImageUrl,
        Medium = ExampleImageUrl,
        Small = ExampleImageUrl,
        Mini = ExampleImageUrl,
    };

    #region Roles

    public static Role Default = new()
    {
        Name = AtlasConstants.DefaultRoleName,
        Permissions = new()
        {
            "tests.default.example",
            AtlasConstants.UserEventCreate,
            AtlasConstants.UserEventEdit,
            AtlasConstants.UserEventDelete,
        }
    };

    public static Role Administrator = new()
    {
        Name = AtlasConstants.AdministratorRoleName,
        Permissions = new()
        {
            "special.administrator",
            "tests.administrator.example"
        }
    };

    public static Role Creator = new()
    {
        Name = nameof(Creator),
        Permissions = new()
        {
            "tests.creator.example",
        }
    };

    public static Role Moderator = new()
    {
        Name = nameof(Moderator),
        Permissions = new()
        {
            "tests.moderator.example",
            AtlasConstants.ModerationEventEdit,
            AtlasConstants.ModerationEventDelete
        }
    };

    public static Role RoleManager = new()
    {
        Name = nameof(RoleManager),
        Permissions = new()
        {
            "tests.rolemanager.example",
            AtlasConstants.AdministratorRoleCreate,
            AtlasConstants.AdministratorRoleEdit,
            AtlasConstants.AdministratorRoleDelete,
        }
    };

    public static Role DummyRole = new()
    {
        Name = nameof(DummyRole),
        Permissions = new()
        {
            "tests.dummy.example",
            AtlasConstants.UserEventEdit
        }
    };

    public static Role[] Roles = new Role[]
    {
        Default,
        Administrator,
        Creator,
        Moderator,
        RoleManager,
        DummyRole,
    };

    #endregion

    #region Users

    /// <summary>
    /// Represents a normal user.
    /// </summary>
    public static User Andromeda = new()
    {
        Id = Guid.NewGuid(),
        Name = nameof(Andromeda),
        Email = "andromeda@vratlas.tech",
        Roles = new List<Role> { Default },
        Icon = ExampleImageVariants,
        IconSourceUrl = ExampleImageUrl,
        Identifiers = new PlatformIdentifiers { DiscordId = RandomNumberString(18) }
    };

    /// <summary>
    /// Represents a user that is not in the database.
    /// </summary>
    public static User AndromedaAlternate = new()
    {
        Id = Guid.NewGuid(),
        Name = "Andromeda Alternate",
        Email = "andromeda-alternate@vratlas.tech",
        Roles = new List<Role> { Default },
        IconSourceUrl = ExampleImageUrl,
        Icon = ExampleImageVariants,
        Identifiers = new PlatformIdentifiers { DiscordId = RandomNumberString(18) }
    };

    /// <summary>
    /// Represents a creator.
    /// </summary>
    public static User Bismuth = new()
    {
        Id = Guid.NewGuid(),
        Name = nameof(Bismuth),
        Email = "bismuth@vratlas.tech",
        Roles = new List<Role> { Creator, Default },
        IconSourceUrl = ExampleImageUrl,
        Icon = ExampleImageVariants,
        Identifiers = new PlatformIdentifiers { DiscordId = RandomNumberString(18) }
    };

    /// <summary>
    /// Represents a user that can manage roles site-wide.
    /// </summary>
    public static User Catharsis = new()
    {
        Id = Guid.NewGuid(),
        Name = nameof(Catharsis),
        Email = "catharsis@vratlas.tech",
        Roles = new List<Role> { RoleManager, Default },
        IconSourceUrl = ExampleImageUrl,
        Icon = ExampleImageVariants,
        Identifiers = new PlatformIdentifiers { DiscordId = RandomNumberString(18) }
    };

    /// <summary>
    /// Represents a site moderator
    /// </summary>
    public static User Diso = new()
    {
        Id = Guid.NewGuid(),
        Name = nameof(Diso),
        Email = "diso@vratlas.tech",
        Roles = new List<Role> { Moderator, Default },
        IconSourceUrl = ExampleImageUrl,
        Icon = ExampleImageVariants,
        Identifiers = new PlatformIdentifiers { DiscordId = RandomNumberString(18) }
    };

    public static User[] Users = new User[]
    {
        Andromeda,
        Bismuth,
        Catharsis,
        Diso,
    };

    public static User[] AllUsers = Users.Append(AndromedaAlternate).ToArray();

    public static void AsTestUser(this HttpClient httpClient, string name)
    {
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Test", name);
    }

    public static string RandomNumberString(int size)
    {
        return string.Join(string.Empty, Enumerable.Range(0, size).Select(i => Random.Shared.Next(0, 10).ToString()));
    }

    #endregion
}