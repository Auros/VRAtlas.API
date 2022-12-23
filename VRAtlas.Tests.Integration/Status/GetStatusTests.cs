using FluentAssertions;
using System.Net;
using System.Net.Http.Json;
using VRAtlas.Models;
using Xunit;

namespace VRAtlas.Tests.Integration.Status;

[CollectionDefinition(StatusCollection.Definition)]
public class GetStatusTests : IClassFixture<VRAtlasFactory>
{
    private readonly HttpClient _httpClient;

	public GetStatusTests(VRAtlasFactory atlasFactory)
	{
		_httpClient = atlasFactory.CreateClient();
	}

	[Fact]
	public async Task GetStatus_ShouldReturnOK()
	{
		// Arrange
		ApiStatus okStatus = new() { Status = "OK" };

		// Act
		using var response = await _httpClient.GetAsync("status");

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.OK);
		var status = await response.Content.ReadFromJsonAsync<ApiStatus>();
		status.Should().BeEquivalentTo(okStatus);
	}
}