using System.Net.Http.Json;
using System.Net;
using VRAtlas.Filters;
using FluentAssertions;

namespace VRAtlas.Tests.Integration;

public class RoleUpdateTests : IClassFixture<AtlasFactory>
{
    private readonly AtlasFactory _atlasFactory;

    public RoleUpdateTests(AtlasFactory atlasFactory)
    {
        _atlasFactory = atlasFactory;
    }

    [Fact]
    public async Task TestUpdateRole_NoUser()
    {
        using var atlas = _atlasFactory.CreateDefaultClient();

        Role payload = new()
        {
            Name = "NewRole",
            Permissions = new List<string> { "some.overridden.permission" }
        };

        var response = await atlas.PostAsJsonAsync("/roles/update", payload);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task TestUpdateRole_UserLacksPermissions()
    {
        using var atlas = _atlasFactory.CreateDefaultClient();
        atlas.AsTestUser(nameof(TestExamples.Andromeda));

        Role payload = new()
        {
            Name = TestExamples.Moderator.Name,
            Permissions = new List<string> { "some.overridden.permission" }
        };

        var response = await atlas.PostAsJsonAsync("/roles/update", payload);
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task TestUpdateRole_UserHasPermissionsAndValidPayload()
    {
        using var atlas = _atlasFactory.CreateDefaultClient();
        atlas.AsTestUser(nameof(TestExamples.Catharsis));

        Role payload = new()
        {
            Name = TestExamples.Moderator.Name,
            Permissions = new List<string> { "some.overridden.permission" }
        };

        var response = await atlas.PostAsJsonAsync("/roles/update", payload);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<Role>();
        body.Should().NotBeNull();
        body!.Name.Should().Be(payload.Name);
        body.Permissions.Should().Contain(payload.Permissions);
    }

    [Fact]
    public async Task TestUpdateRole_UserHasPermissionsButModifyingDefaultRole()
    {
        using var atlas = _atlasFactory.CreateDefaultClient();
        atlas.AsTestUser(nameof(TestExamples.Catharsis));

        Role payload = new()
        {
            Name = AtlasConstants.DefaultRoleName,
            Permissions = new List<string> { "some.overridden.permission" }
        };

        var response = await atlas.PostAsJsonAsync("/roles/update", payload);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadFromJsonAsync<Error>();
        body.Should().NotBeNull();
        body!.ErrorName.Should().Be($"The role '{AtlasConstants.DefaultRoleName}' cannot be modified through the API.");
    }

    [Fact]
    public async Task TestUpdateRole_UserHasPermissionsButInvalidPayload()
    {
        using var atlas = _atlasFactory.CreateDefaultClient();
        atlas.AsTestUser(nameof(TestExamples.Catharsis));

        Role payload = new()
        {
            Name = TestExamples.Moderator.Name,
            Permissions = new List<string> { Enumerable.Range(0, 257).Select(i => i.ToString()).Aggregate((a, b) => a + b) } // Super long permission name, not valid.
        };

        var response = await atlas.PostAsJsonAsync("/roles/update", payload);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var body = await response.Content.ReadFromJsonAsync<FilterValidationResponse>();
        body.Should().NotBeNull();
        body!.Error.Should().Be("Validation Failed");
        body.Errors.Should().HaveCount(1); // Permission is too long.
    }

    [Fact]
    public async Task TestUpdateRole_UserHasPermissionsButRoleDoesNotExist()
    {
        using var atlas = _atlasFactory.CreateDefaultClient();
        atlas.AsTestUser(nameof(TestExamples.Catharsis));

        Role payload = new()
        {
            Name = "RoleThatDoesNotExist",
            Permissions = new List<string> { "some.overridden.permission" }
        };

        var response = await atlas.PostAsJsonAsync("/roles/update", payload);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadFromJsonAsync<Error>();
        body.Should().NotBeNull();
        body!.ErrorName.Should().Be("The role 'RoleThatDoesNotExist' does not exist.");
    }
}