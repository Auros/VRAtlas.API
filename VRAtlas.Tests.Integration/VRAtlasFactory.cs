using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.Containers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using VRAtlas.Tests.Integration.Servers;
using Xunit;

namespace VRAtlas.Tests.Integration;

public class VRAtlasFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly CloudflareApiServer _cloudflareApiServer;
    private readonly PostgreSqlTestcontainer _mainDatabaseContainer;
    private readonly PostgreSqlTestcontainer _quartzDatabaseContainer;

    public VRAtlasFactory()
    {
        _cloudflareApiServer = new CloudflareApiServer();

        _mainDatabaseContainer = new TestcontainersBuilder<PostgreSqlTestcontainer>()
            .WithDatabase(new PostgreSqlTestcontainerConfiguration("postgres:latest")
            {
                Database = "vratlas_tests",
                Username = nameof(VRAtlas),
                Password = nameof(VRAtlas),
            })
            .Build();

        _quartzDatabaseContainer = new TestcontainersBuilder<PostgreSqlTestcontainer>()
            .WithDatabase(new PostgreSqlTestcontainerConfiguration("postgres:latest")
            {
                Database = "vratlas_job_store_tests",
                Username = nameof(VRAtlas),
                Password = nameof(VRAtlas),
            })
            .Build();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Remove all logging providers
        builder.ConfigureLogging(logging => logging.ClearProviders());

        builder.ConfigureAppConfiguration(config =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "ConnectionStrings:Main", _mainDatabaseContainer.ConnectionString },
                { "ConnectionStrings:Quartz", _quartzDatabaseContainer.ConnectionString },
                { "Cloudflare:ApiUrl", _cloudflareApiServer.Url.ToString() },
                { "ConnectionStrings:ApiKey", nameof(VRAtlas) },
            });
        });
    }

    public async Task InitializeAsync()
    {
        _cloudflareApiServer.Start("abcdefghijklmnopqrstuvwxyz");
        await _mainDatabaseContainer.StartAsync();
        await _quartzDatabaseContainer.StartAsync();

        // Initialize the test job database tables
        await _quartzDatabaseContainer.ExecScriptAsync(File.ReadAllText("quartz.sql"));
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        _cloudflareApiServer.Dispose();
        await _mainDatabaseContainer.StopAsync();
        await _quartzDatabaseContainer.StopAsync();
    }
}