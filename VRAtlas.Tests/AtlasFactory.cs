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
using VRAtlas.Tests.Setup;

namespace VRAtlas.Tests;

public class AtlasFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlTestcontainer _databaseContainer;

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
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<AtlasContext>();
            services.RemoveAll<IAuthService>();
            services.AddScoped<IAuthService, TestAuthService>();
            services.AddDbContext<AtlasContext>(options =>
            {
                options.UseNpgsql(_databaseContainer.ConnectionString, npgsqlOptions =>
                {
                    npgsqlOptions.UseNodaTime();
                });
            });
            services.AddAuthentication("Test")
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test", options => { });
        });
    }

    public async Task InitializeAsync()
    {
        await _databaseContainer.StartAsync();
        await SetupTestData();
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await _databaseContainer.StopAsync();
    }

    private async Task SetupTestData()
    {
        await using var scope = Services.CreateAsyncScope();
        var atlasContext = scope.ServiceProvider.GetRequiredService<AtlasContext>();
        atlasContext.Users.RemoveRange(atlasContext.Users);
        atlasContext.Users.AddRange(TestExamples.Users);
        await atlasContext.SaveChangesAsync();
    }
}