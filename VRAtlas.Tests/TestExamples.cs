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
            "user.event.create",
            "user.event.edit",
            "user.event.delete"
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
            "moderation.event.edit",
            "moderation.event.delete"
        }
    };

    public static Role[] Roles = new Role[]
    {
        Default,
        Administrator,
        Creator,
        Moderator,
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

    public static User[] Users = new User[]
    {
        Andromeda,
        Bismuth,
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