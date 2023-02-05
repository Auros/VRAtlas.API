using FluentAssertions;
using System.Net.Http.Json;
using Xunit;

namespace VRAtlas.Tests.Integration.User;

[CollectionDefinition(UserCollection.Definition)]
public class GetByIdTests : IClassFixture<VRAtlasFactory>
{
    private readonly HttpClient _httpClient;

    public GetByIdTests(VRAtlasFactory atlasFactory)
    {
        _httpClient = atlasFactory.CreateClient();
    }

    [Fact]
    public async Task GetById_ShouldReturnUser_WithExistingId()
    {
        // Arrange
        var accessToken = await _httpClient.LoginDefaultTestUser();

        var userMsg = TestConstants.CreateHttpMessage("users/@me", accessToken, HttpMethod.Get);
        var expectedUser = (await (await _httpClient.SendAsync(userMsg)).Content.ReadFromJsonAsync<Models.User>())!;

        // Act
        using var response = await _httpClient.GetAsync($"users/{expectedUser.Id}");

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        var user = await response.Content.ReadFromJsonAsync<Models.User>();

        user.Should().NotBeNull();
        user!.Id.Should().Be(user.Id);
        user!.Username.Should().Be(expectedUser.Username);
    }

    [Fact]
    public async Task GetById_ShouldReturnNull_WithNonExistantId()
    {
        // Act
        using var response = await _httpClient.GetAsync($"users/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound);
    }
}