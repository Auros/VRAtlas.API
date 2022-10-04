using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.Containers;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
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
            .WithName("redis")
            .WithEnvironment("REDIS_PASSWORD", nameof(VRAtlas))
            .WithPortBinding(_redisPort, 6379)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(6379))
            .Build();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<AtlasContext>();
            services.RemoveAll<IAuthService>();
            services.RemoveAll<IConnectionMultiplexer>();
            services.AddScoped<IAuthService, TestAuthService>();
            services.AddDbContext<AtlasContext>(options =>
            {
                options.UseNpgsql(_databaseContainer.ConnectionString, npgsqlOptions =>
                {
                    npgsqlOptions.UseNodaTime();
                });
            });
            services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect($"localhost:{_redisPort},password={nameof(VRAtlas)},allowAdmin=true"));
            services.AddAuthentication("Test")
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test", options => { });
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
        atlasContext.Users.RemoveRange(atlasContext.Users);
        atlasContext.Users.AddRange(TestExamples.Users);
        await atlasContext.SaveChangesAsync();
    }
}