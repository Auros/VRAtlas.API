using FluentAssertions;
using System.Net;
using System.Net.Http.Json;

namespace VRAtlas.Tests.Integration;

public class UserTests : IClassFixture<AtlasFactory>
{
    private readonly AtlasFactory _atlasFactory;

    public UserTests(AtlasFactory atlasFactory)
    {
        _atlasFactory = atlasFactory;
    }

    [Fact]
    public async Task TestGetLoggedInUser_WithNoUser()
    {
        using var atlas = _atlasFactory.CreateDefaultClient();

        var response = await atlas.GetAsync("/users/@me");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task TestGetLoggedInUser_WithNoMatchingClaims()
    {
        using var atlas = _atlasFactory.CreateDefaultClient();
        atlas.AsTestUser(nameof(TestExamples.AndromedaAlternate));

        var response = await atlas.GetAsync("/users/@me");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task TestGetLoggedInUser_WithMatchingClaims()
    {
        using var atlas = _atlasFactory.CreateDefaultClient();
        atlas.AsTestUser(nameof(TestExamples.Andromeda));

        var response = await atlas.GetAsync("/users/@me");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<User>();
        body!.Name.Should().Be(TestExamples.Andromeda.Name);
        body.Id.Should().Be(TestExamples.Andromeda.Id);
    }

    [Fact]
    public async Task TestGetUser_Exists()
    {
        using var atlas = _atlasFactory.CreateDefaultClient();

        var response = await atlas.GetAsync($"/user/{TestExamples.Andromeda.Id}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<User>();
        body!.Name.Should().Be(TestExamples.Andromeda.Name);
        body.Id.Should().Be(TestExamples.Andromeda.Id);
    }

    [Fact]
    public async Task TestGetUser_NonExistant()
    {
        using var atlas = _atlasFactory.CreateDefaultClient();

        var response = await atlas.GetAsync($"/user/{TestExamples.AndromedaAlternate.Id}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}