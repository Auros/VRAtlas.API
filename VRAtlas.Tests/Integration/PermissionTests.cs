using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace VRAtlas.Tests.Integration;

public class PermissionTests : IClassFixture<AtlasFactory>
{
    private readonly AtlasFactory _atlasFactory;

    public PermissionTests(AtlasFactory atlasFactory)
    {
        _atlasFactory = atlasFactory;
    }

    [Fact]
    public async Task TestDefaultPermissionAssigned()
    {
        using var atlas = _atlasFactory.CreateDefaultClient();
        await using var scope = _atlasFactory.Services.CreateAsyncScope();
        var atlasContext = scope.ServiceProvider.GetRequiredService<AtlasContext>();

        var role = await atlasContext.Roles.FirstOrDefaultAsync(r => r.Name == AtlasConstants.DefaultRoleName);
        role.Should().NotBeNull();

        role!.Permissions.Should().NotBeEmpty();
        role.Permissions.Should().Contain("tests.default.example");
    }
}