using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NodaTime;
using NSubstitute;
using VRAtlas.Logging;
using VRAtlas.Models;
using VRAtlas.Services;
using Xunit;

namespace VRAtlas.Tests.Unit.Services;

public sealed class GroupServiceTests : IClassFixture<AtlasFixture>
{
    private readonly GroupService _sut;
    private readonly AtlasContext _atlasContext;
    private readonly IClock _clock = Substitute.For<IClock>();
    private readonly IAtlasLogger<GroupService> _atlasLogger = Substitute.For<IAtlasLogger<GroupService>>();

    public GroupServiceTests(AtlasFixture atlasFixture)
    {
        _atlasContext = atlasFixture.Context;
        _sut = new GroupService(_clock, _atlasLogger, _atlasContext);
    }

    [Fact]
    public async Task GetGroupByIdAsync_ShouldReturnGroup_WithValidId()
    {
        // Arrange
        var group = AtlasFakes.Group.Generate();
        _atlasContext.Groups.Add(group);
        await _atlasContext.SaveChangesAsync();

        // Act
        var fetchedGroup = await _sut.GetGroupByIdAsync(group.Id);

        // Assert
        fetchedGroup.Should().NotBeNull();
        fetchedGroup!.Id.Should().Be(group.Id);

        // Cleanup
        _atlasContext.Remove(group);
        await _atlasContext.SaveChangesAsync();
    }

    [Fact]
    public async Task GetGroupByIdAsync_ShouldReturnNull_WithNonExistantId()
    {
        // Arrange
        var idThatDoesntExist = Guid.NewGuid();

        // Act
        var fetchedGroup = await _sut.GetGroupByIdAsync(idThatDoesntExist);

        // Assert
        fetchedGroup.Should().BeNull();
    }

    [Fact]
    public async Task CreateGroupAsync_ShouldReturnGroup_WithValidInput()
    {
        // Arrange
        var user = AtlasFakes.User.Generate();
        _atlasContext.Users.Add(user);
        await _atlasContext.SaveChangesAsync();

        var name = nameof(CreateGroupAsync_ShouldReturnGroup_WithValidInput);
        var description = nameof(CreateGroupAsync_ShouldReturnGroup_WithValidInput);
        var icon = Guid.NewGuid();
        var banner = Guid.NewGuid();

        // Act
        var group = await _sut.CreateGroupAsync(name, description, icon, banner, user.Id);

        // Assert
        group.Should().NotBeNull();
        group.Name.Should().Be(name);
        group.Description.Should().Be(description);
        group.Icon.Should().Be(icon);
        group.Banner.Should().Be(banner);
        group.Members.Should().ContainSingle();
        group.Members.Should().Contain(c => c.User!.Id == user.Id, "Cannot find user id.");
        group.Members.Should().Contain(c => c.Role == Models.GroupMemberRole.Owner, "First group member is not the owner.");

        _atlasLogger.Received(1).LogInformation(Arg.Is("User {UserId} created group {GroupId}"), Arg.Is(user.Id), Arg.Is(group.Id));

        var existsInDb = await _atlasContext.Groups.AnyAsync(g => g.Id == group.Id);
        existsInDb.Should().BeTrue();

        // Cleanup
        _atlasContext.Users.Remove(user);
        _atlasContext.Groups.Remove(group);
        await _atlasContext.SaveChangesAsync();
    }

    [Fact]
    public async Task CreateGroupAsync_ShouldThrowException_WithInUseName()
    {
        // Arrange
        var group = AtlasFakes.Group.Generate();
        var owner = group.Members[0];
        _atlasContext.Groups.Add(group);
        await _atlasContext.SaveChangesAsync();

        // Act
        Func<Task> act = async () => { await _sut.CreateGroupAsync(group.Name, string.Empty, Guid.NewGuid(), Guid.NewGuid(), owner.Id); };

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage($"Group name '{group.Name}' already exists");
        _atlasLogger.Received(1).LogWarning(Arg.Is("A group creation event was attempted with an already existing name of {GroupName}"), Arg.Is(group.Name));

        // Cleanup
        _atlasContext.Groups.Remove(group);
        _atlasContext.Users.Remove(owner.User!);
        await _atlasContext.SaveChangesAsync();
    }

    [Fact]
    public async Task CreateGroupAsync_ShouldThrowException_WithNonExistantOwnerId()
    {
        // Arrange
        var id = Guid.NewGuid();

        // Act
        Func<Task> act = async () => { await _sut.CreateGroupAsync("Group", string.Empty, Guid.NewGuid(), Guid.NewGuid(), id); };

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage($"A user with the id '{id}' does not exist.");
        _atlasLogger.Received(1).LogWarning(Arg.Is("Unable to create a group, could not find the owning user"));
    }

    [Fact]
    public async Task AddGroupMemberAsync_ShouldReturnUpdatedGroup_WithValidInputs()
    {
        // Arrange
        var group = AtlasFakes.Group.Generate();
        var owner = group.Members[0];
        var addedUser = AtlasFakes.User.Generate();
        _clock.GetCurrentInstant().Returns(_ => SystemClock.Instance.GetCurrentInstant());

        _atlasContext.Groups.Add(group);
        _atlasContext.Users.Add(addedUser);
        await _atlasContext.SaveChangesAsync();
        
        // Act
        var updatedGroup = await _sut.AddGroupMemberAsync(group.Id, addedUser.Id, GroupMemberRole.Standard);
        
        // Assert
        updatedGroup.Members.Should().Contain(m => m.User!.Id == addedUser.Id);
        updatedGroup.Members.First(m => m.User!.Id == addedUser.Id).Role.Should().Be(GroupMemberRole.Standard);

        // Cleanup
        _atlasContext.Groups.Remove(group);
        _atlasContext.Users.Remove(addedUser);
        _atlasContext.Users.Remove(owner.User!);
        await _atlasContext.SaveChangesAsync();
    }

    [Fact]
    public async Task AddGroupMemberAsync_ShouldThrowException_WhileApplyingOwnerRole()
    {
        // Arrange
        var group = AtlasFakes.Group.Generate();
        var owner = group.Members[0];
        var addedUser = AtlasFakes.User.Generate();
        _clock.GetCurrentInstant().Returns(_ => SystemClock.Instance.GetCurrentInstant());

        _atlasContext.Groups.Add(group);
        _atlasContext.Users.Add(addedUser);
        await _atlasContext.SaveChangesAsync();

        // Act
        Func<Task> act = async () => { await _sut.AddGroupMemberAsync(group.Id, addedUser.Id, GroupMemberRole.Owner); };

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("Cannot add or modify a member to the Owner role");

        // Cleanup
        _atlasContext.Groups.Remove(group);
        _atlasContext.Users.Remove(addedUser);
        _atlasContext.Users.Remove(owner.User!);
        await _atlasContext.SaveChangesAsync();
    }

    [Fact]
    public async Task RemoveGroupMemberAsync_ShouldReturnUpdatedGroup_WithValidInputs()
    {
        // Arrange
        var group = AtlasFakes.Group.Generate();
        group.Members.Add(AtlasFakes.GroupMember.Generate());
        var owner = group.Members[0];
        var target = group.Members[1];
        target.Role = GroupMemberRole.Manager;

        _atlasContext.Groups.Add(group);
        await _atlasContext.SaveChangesAsync();

        // Act
        var updatedGroup = await _sut.RemoveGroupMemberAsync(group.Id, target.User!.Id);

        // Assert
        updatedGroup.Members.Should().ContainSingle();
        updatedGroup.Members.First(m => m.User!.Id == owner.User!.Id).Role.Should().Be(GroupMemberRole.Owner);

        // Cleanup
        _atlasContext.Groups.Remove(group);
        _atlasContext.Users.Remove(owner.User!);
        _atlasContext.Users.Remove(target.User);
        await _atlasContext.SaveChangesAsync();
    }

    [Fact]
    public async Task RemoveGroupMemberAsync_ShouldThrowException_WhileRemovingOwner()
    {
        // Arrange
        var group = AtlasFakes.Group.Generate();
        var owner = group.Members[0];

        _atlasContext.Groups.Add(group);
        await _atlasContext.SaveChangesAsync();

        // Act
        Func<Task> act = async () => { await _sut.RemoveGroupMemberAsync(group.Id, owner.User!.Id); };

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("Cannot remove a member with the Owner role");

        // Cleanup
        _atlasContext.Groups.Remove(group);
        _atlasContext.Users.Remove(owner.User);
        await _atlasContext.SaveChangesAsync();
    }

    [Fact]
    public async Task GetGroupMemberRoleAsync_ShouldReturnCorrectRole_WithValidInputs()
    {
        // Arrange
        var group = AtlasFakes.Group.Generate();
        var owner = group.Members[0];
        _atlasContext.Groups.Add(group);
        await _atlasContext.SaveChangesAsync();

        // Act
        var role = await _sut.GetGroupMemberRoleAsync(group.Id, owner.User!.Id);

        // Assert
        role.Should().NotBeNull();
        role.Should().Be(owner.Role);

        // Cleanup
        _atlasContext.Groups.Remove(group);
        _atlasContext.Users.Remove(owner.User!);
        await _atlasContext.SaveChangesAsync();
    }

    [Fact]
    public async Task GetGroupMemberRoleAsync_ShouldReturnNull_WithNonExistantGroup()
    {
        // Arrange
        var user = AtlasFakes.User.Generate();
        _atlasContext.Users.Add(user);
        await _atlasContext.SaveChangesAsync();

        // Act
        var role = await _sut.GetGroupMemberRoleAsync(Guid.NewGuid(), user.Id);

        // Assert
        role.Should().BeNull();

        // Cleanup
        _atlasContext.Users.Remove(user);
        await _atlasContext.SaveChangesAsync();
    }

    [Fact]
    public async Task GetGroupMemberRoleAsync_ShouldReturnNull_WithNonExistantUser()
    {
        // Arrange
        var group = AtlasFakes.Group.Generate();
        _atlasContext.Groups.Add(group);
        await _atlasContext.SaveChangesAsync();

        // Act
        var role = await _sut.GetGroupMemberRoleAsync(group.Id, Guid.NewGuid());

        // Assert
        role.Should().BeNull();

        // Cleanup
        _atlasContext.Groups.Remove(group);
        _atlasContext.Users.Remove(group.Members[0].User!);
        await _atlasContext.SaveChangesAsync();
    }

    [Fact]
    public async Task GroupExistsAsync_ShouldReturnTrue_WithExistingGroup()
    {
        // Arrange
        var group = AtlasFakes.Group.Generate();
        _atlasContext.Groups.Add(group);
        await _atlasContext.SaveChangesAsync();

        // Act
        var result = await _sut.GroupExistsAsync(group.Id);

        // Assert
        result.Should().BeTrue();

        // Cleanup
        _atlasContext.Groups.Remove(group);
        _atlasContext.Users.Remove(group.Members[0].User!);
        await _atlasContext.SaveChangesAsync();
    }

    [Fact]
    public async Task GroupExistsAsync_ShouldReturnFalse_WithNonExistantGroup()
    {
        // Arrange
        var groupId = Guid.NewGuid();

        // Act
        var result = await _sut.GroupExistsAsync(groupId);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task GroupExistsByNameAsync_ShouldReturnTrue_WithExistingGroup()
    {
        // Arrange
        var group = AtlasFakes.Group.Generate();
        _atlasContext.Groups.Add(group);
        await _atlasContext.SaveChangesAsync();

        // Act
        var result = await _sut.GroupExistsByNameAsync(group.Name);

        // Assert
        result.Should().BeTrue();

        // Cleanup
        _atlasContext.Groups.Remove(group);
        _atlasContext.Users.Remove(group.Members[0].User!);
        await _atlasContext.SaveChangesAsync();
    }

    [Fact]
    public async Task GroupExistsByNameAsync_ShouldReturnFalse_WithNonExistantGroup()
    {
        // Arrange
        var name = "this group name totally does not exist";

        // Act
        var result = await _sut.GroupExistsByNameAsync(name);

        // Assert
        result.Should().BeFalse();
    }


    [Fact]
    public async Task GetAllUserGroupsAsync_ShouldReturnUserGroups_WithExistingUser()
    {
        // Arrange
        var groupA = AtlasFakes.Group.Generate();
        var groupB = AtlasFakes.Group.Generate();
        var user = groupA.Members[0].User!;
        groupB.Members[0].User = user;
        groupA.Members[0].Role = GroupMemberRole.Manager;
        _atlasContext.Groups.Add(groupA);
        _atlasContext.Groups.Add(groupB);
        await _atlasContext.SaveChangesAsync();

        // Act
        var groups = await _sut.GetAllUserGroupsAsync(user.Id);

        // Assert
        groups.Should().NotBeNullOrEmpty();
        groups.Should().HaveCount(2);
        groups.First().Id.Should().Be(groupB.Id); // The first element should be the second group, as the user is the Owner in that group.
        groups.Last().Id.Should().Be(groupA.Id);

        // Cleanup
        _atlasContext.Groups.Remove(groupA);
        _atlasContext.Groups.Remove(groupB);
        _atlasContext.Users.Remove(user);
        await _atlasContext.SaveChangesAsync();
    }

    [Fact]
    public async Task GetAllUserGroupsAsync_ShouldReturnNoGroups_WithNonExistant()
    {
        // Arrange
        var group = AtlasFakes.Group.Generate();
        _atlasContext.Groups.Add(group);
        await _atlasContext.SaveChangesAsync();

        // Act
        var groups = await _sut.GetAllUserGroupsAsync(Guid.NewGuid());

        // Assert
        groups.Should().BeEmpty();

        // Cleanup
        _atlasContext.Groups.Remove(group);
        _atlasContext.Users.Remove(group.Members[0].User!);
        await _atlasContext.SaveChangesAsync();
    }

    [Fact]
    public async Task ModifyGroupAsync_ShouldReturnUpdatedGroup_WithValidInputs()
    {
        // Arrange
        var group = AtlasFakes.Group.Generate();
        _atlasContext.Groups.Add(group);
        await _atlasContext.SaveChangesAsync();

        var description = "My new description";
        var icon = Guid.NewGuid();
        var banner = Guid.NewGuid();

        // Act
        var updatedGroup = await _sut.ModifyGroupAsync(group.Id, description, icon, banner);

        // Assert
        updatedGroup.Description.Should().Be(description);
        updatedGroup.Icon.Should().Be(icon);
        updatedGroup.Banner.Should().Be(banner);

        _atlasLogger.Received(1).LogInformation(Arg.Is("Group {GroupId} was updated"), Arg.Is(group.Id));
    }

    [Fact]
    public async Task ModifyGroupAsync_ShouldThrowException_WithNonExistantGroup()
    {
        // Arrange
        var groupId = Guid.NewGuid();

        // Act
        Func<Task> act = async () => { await _sut.ModifyGroupAsync(groupId, string.Empty, Guid.NewGuid(), Guid.NewGuid()); };

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage($"Could not find group with id '{groupId}'.");
    }
}