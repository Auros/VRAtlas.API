using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NodaTime;
using System.Security.Claims;
using VRAtlas.Models;
using VRAtlas.Services;
using Xunit;

namespace VRAtlas.Tests.Unit.Services;

public sealed class UserServiceTests : IClassFixture<AtlasFixture>
{
    private readonly UserService _sut;
    private readonly AtlasContext _atlasContext;

    public UserServiceTests(AtlasFixture atlasFixture)
    {
        _sut = new UserService(atlasFixture.Context);
        _atlasContext = atlasFixture.Context;
    }

    [Fact]
    public async Task GetUserAsync_ShouldReturnUser_WithValidPrincipal()
    {
        // Arrange
        var user = AtlasFakes.User.Generate();

        _atlasContext.Users.Add(user);
        await _atlasContext.SaveChangesAsync();

        ClaimsPrincipal principal = new(new ClaimsIdentity[] { new ClaimsIdentity(new Claim[] { new Claim(ClaimTypes.NameIdentifier, user.SocialId) }) });

        // Act
        var fetchedUser = await _sut.GetUserAsync(principal);

        // Assert
        fetchedUser.Should().NotBeNull();
        fetchedUser!.Id.Should().Be(user.Id);
        fetchedUser!.Username.Should().Be(user.Username);

        // Cleanup
        _atlasContext.Remove(user);
        await _atlasContext.SaveChangesAsync();
    }

    [Fact]
    public async Task GetUserAsync_ShouldReturnNull_WithPrincipalContainingNoIdentifier()
    {
        // Arrange
        var user = AtlasFakes.User.Generate();

        _atlasContext.Users.Add(user);
        await _atlasContext.SaveChangesAsync();

        ClaimsPrincipal principal = new();

        // Act
        var fetchedUser = await _sut.GetUserAsync(principal);

        // Assert
        fetchedUser.Should().BeNull();

        // Cleanup
        _atlasContext.Remove(user);
        await _atlasContext.SaveChangesAsync();
    }

    [Fact]
    public async Task GetUserAsync_ShouldReturnNull_WithPrincipalContainingUnknownIdentifier()
    {
        // Arrange
        var user = AtlasFakes.User.Generate();

        _atlasContext.Users.Add(user);
        await _atlasContext.SaveChangesAsync();

        ClaimsPrincipal principal = new(new ClaimsIdentity[] { new ClaimsIdentity(new Claim[] { new Claim(ClaimTypes.NameIdentifier, "definitely not the an existing user id") }) });

        // Act
        var fetchedUser = await _sut.GetUserAsync(principal);

        // Assert
        fetchedUser.Should().BeNull();

        // Cleanup
        _atlasContext.Remove(user);
        await _atlasContext.SaveChangesAsync();
    }

    [Fact]
    public async Task GetUserAsync_ShouldReturnUser_WithExistingUserId()
    {
        // Arrange
        var user = AtlasFakes.User.Generate();

        _atlasContext.Users.Add(user);
        await _atlasContext.SaveChangesAsync();

        // Act
        var fetchedUser = await _sut.GetUserAsync(user.Id);

        // Assert
        fetchedUser.Should().NotBeNull();
        fetchedUser!.Id.Should().Be(user.Id);
        fetchedUser!.Username.Should().Be(user.Username);

        // Cleanup
        _atlasContext.Remove(user);
        await _atlasContext.SaveChangesAsync();
    }

    [Fact]
    public async Task GetUserAsync_ShouldReturnNull_WithUnknownUserId()
    {
        // Arrange
        Guid randomId = Guid.NewGuid();

        // Act
        var fetchedUser = await _sut.GetUserAsync(randomId);

        // Assert
        fetchedUser.Should().BeNull();
    }
}
