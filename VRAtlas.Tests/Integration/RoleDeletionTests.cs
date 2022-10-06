using System.Net.Http.Json;
using System.Net;
using FluentAssertions;

namespace VRAtlas.Tests.Integration;

public class RoleDeletionTests : IClassFixture<AtlasFactory>
{
    private readonly AtlasFactory _atlasFactory;

    public RoleDeletionTests(AtlasFactory atlasFactory)
    {
        _atlasFactory = atlasFactory;
    }

    [Fact]
    public async Task TestDeleteRole_NoUser()
    {
        using var atlas = _atlasFactory.CreateDefaultClient();

        var response = await atlas.DeleteAsync($"/roles/delete/{TestExamples.Moderator.Name}");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task TestDeleteRole_UserLacksPermissions()
    {
        using var atlas = _atlasFactory.CreateDefaultClient();
        atlas.AsTestUser(nameof(TestExamples.Andromeda));

        var response = await atlas.DeleteAsync($"/roles/delete/{TestExamples.Moderator.Name}");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task TestDeleteRole_UserHasPermissions_DeletingInUseRole()
    {
        using var atlas = _atlasFactory.CreateDefaultClient();
        atlas.AsTestUser(nameof(TestExamples.Catharsis));

        var response = await atlas.DeleteAsync($"/roles/delete/{TestExamples.Moderator.Name}");
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task TestDeleteRole_UserHasPermissions_DeletingUnusedRole()
    {
        using var atlas = _atlasFactory.CreateDefaultClient();
        atlas.AsTestUser(nameof(TestExamples.Catharsis));

        var response = await atlas.DeleteAsync($"/roles/delete/{TestExamples.DummyRole.Name}");
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task TestDeleteRole_UserHasPermissionsButRoleDoesNotExist()
    {
        using var atlas = _atlasFactory.CreateDefaultClient();
        atlas.AsTestUser(nameof(TestExamples.Catharsis));

        var response = await atlas.DeleteAsync("/roles/delete/RoleThatDoesNotExist");
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadFromJsonAsync<Error>();
        body.Should().NotBeNull();
        body!.ErrorName.Should().Be("The role 'RoleThatDoesNotExist' does not exist.");
    }
}