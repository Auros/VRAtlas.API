using FluentAssertions;
using System.Net;
using System.Net.Http.Json;
using VRAtlas.Filters;

namespace VRAtlas.Tests.Integration;

public class RoleCreationTests : IClassFixture<AtlasFactory>
{
    private readonly AtlasFactory _atlasFactory;

    public RoleCreationTests(AtlasFactory atlasFactory)
    {
        _atlasFactory = atlasFactory;
    }

    [Fact]
    public async Task TestCreateRole_NoUser()
    {
        using var atlas = _atlasFactory.CreateDefaultClient();

        Role payload = new()
        {
            Name = "NewRole",
            Permissions = new List<string> { "some.new.permission" }
        };

        var response = await atlas.PostAsJsonAsync("/roles/create", payload);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task TestCreateRole_UserLacksPermissions()
    {
        using var atlas = _atlasFactory.CreateDefaultClient();
        atlas.AsTestUser(nameof(TestExamples.Andromeda));

        Role payload = new()
        {
            Name = "NewRole",
            Permissions = new List<string> { "some.new.permission" }
        };

        var response = await atlas.PostAsJsonAsync("/roles/create", payload);
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task TestCreateRole_UserHasPermissionsAndValidPayload()
    {
        using var atlas = _atlasFactory.CreateDefaultClient();
        atlas.AsTestUser(nameof(TestExamples.Catharsis));

        Role payload = new()
        {
            Name = "NewRole",
            Permissions = new List<string> { "some.new.permission" }
        };

        var response = await atlas.PostAsJsonAsync("/roles/create", payload);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<Role>();
        body.Should().NotBeNull();
        body!.Name.Should().Be(payload.Name);
        body.Permissions.Should().Contain(payload.Permissions);
    }

    [Fact]
    public async Task TestCreateRole_UserHasPermissionsButNonUniqueName()
    {
        using var atlas = _atlasFactory.CreateDefaultClient();
        atlas.AsTestUser(nameof(TestExamples.Catharsis));

        Role payload = new()
        {
            Name = AtlasConstants.AdministratorRoleName,
            Permissions = new List<string> { "some.new.permission" }
        };

        var response = await atlas.PostAsJsonAsync("/roles/create", payload);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task TestCreateRole_UserHasPermissionsButInvalidPayload()
    {
        using var atlas = _atlasFactory.CreateDefaultClient();
        atlas.AsTestUser(nameof(TestExamples.Catharsis));

        Role payload = new()
        {
            Name = string.Empty, // Empty string, can't be role name
            Permissions = new List<string> { Enumerable.Range(0, 257).Select(i => i.ToString()).Aggregate((a, b) => a + b) } // Super long permission name, not valid.
        };

        var response = await atlas.PostAsJsonAsync("/roles/create", payload);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var body = await response.Content.ReadFromJsonAsync<FilterValidationResponse>();
        body.Should().NotBeNull();
        body!.Error.Should().Be("Validation Failed");
        body.Errors.Should().HaveCount(2); // Name is empty and an added permission is too long.
    }
}