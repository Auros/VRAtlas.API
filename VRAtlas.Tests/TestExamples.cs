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

    public static User Andromeda = new()
    {
        Id = new Guid("292445c3-5bfd-4ffe-8d9c-fdf64b395757"),
        Email = "andromeda@vratlas.tech",
        Name = "Andromeda",
        IconSourceUrl = ExampleImageUrl,
        Icon = ExampleImageVariants,
        Identifiers = new PlatformIdentifiers { DiscordId = "218571218545016832" }
    };

    public static User AndromedaAlternate = new()
    {
        Id = new Guid("39da2cc0-0372-49c4-97cb-87873220031a"),
        Email = "andromeda-alternate@vratlas.tech",
        Name = "Andromeda Alternate",
        IconSourceUrl = ExampleImageUrl,
        Icon = ExampleImageVariants,
        Identifiers = new PlatformIdentifiers { DiscordId = "277200664424218634" }
    };

    public static User[] Users = new User[]
    {
        Andromeda
    };

    public static User[] AllUsers = Users.Append(AndromedaAlternate).ToArray();

    public static void AsTestUser(this HttpClient httpClient, string name)
    {
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Test", name);
    }
}