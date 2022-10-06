using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.Containers;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using StackExchange.Redis;
using VRAtlas.Tests.Setup;

namespace VRAtlas.Tests;

public class AtlasFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly TestcontainersContainer _redisContainer;
    private readonly PostgreSqlTestcontainer _databaseContainer;
    private readonly int _redisPort = Random.Shared.Next(10_000, 60_000);

    public AtlasFactory()
    {
        _databaseContainer = new TestcontainersBuilder<PostgreSqlTestcontainer>()
            .WithDatabase(new PostgreSqlTestcontainerConfiguration
            {
                Database = "vratlas_tests",
                Username = nameof(VRAtlas),
                Password = nameof(VRAtlas),
            })
            .Build();

        _redisContainer = new TestcontainersBuilder<TestcontainersContainer>()
            .WithImage("redis:latest")
            .WithName($"redis-test-{string.Join(string.Empty, _redisPort.ToString().Select(r => ((int)r).ToString()))}")
            .WithEnvironment("REDIS_PASSWORD", nameof(VRAtlas))
            .WithPortBinding(_redisPort, 6379)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(6379))
            .Build();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration(config =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "ConnectionStrings:Database", _databaseContainer.ConnectionString },
                { "ConnectionStrings:Redis", $"localhost:{_redisPort},password={nameof(VRAtlas)},allowAdmin=true" },
                { "DefaultPermissions:0", "tests.default.example" },
                { "DefaultPermissions:1", "user.event.create" },
                { "DefaultPermissions:2", "user.event.edit" },
                { "DefaultPermissions:3", "user.event.delete" },
            });
        });

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IAuthService>();
            services.AddScoped<IAuthService, TestAuthService>();
            services.AddAuthentication("Test").AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test", options => { });
        });
    }

    public async Task InitializeAsync()
    {
        await _redisContainer.StartAsync();
        await _databaseContainer.StartAsync();
        await SetupTestData();
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await _databaseContainer.StopAsync();
        await _redisContainer.StopAsync();
    }

    private async Task SetupTestData()
    {
        var multiplexer = Services.GetRequiredService<IConnectionMultiplexer>();
        await multiplexer.GetServers().First().FlushAllDatabasesAsync();

        await using var scope = Services.CreateAsyncScope();
        var atlasContext = scope.ServiceProvider.GetRequiredService<AtlasContext>();

        // Load in our test data from TestExamples.cs
        atlasContext.Roles.RemoveRange(atlasContext.Roles);
        atlasContext.Roles.AddRange(TestExamples.Roles);

        atlasContext.Users.RemoveRange(atlasContext.Users);
        atlasContext.Users.AddRange(TestExamples.Users);

        await atlasContext.SaveChangesAsync();
    }
}