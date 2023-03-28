using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.Containers;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Quartz;
using Testcontainers.PostgreSql;
using VRAtlas.Authorization;
using VRAtlas.Options;
using VRAtlas.Tests.Integration.Servers;
using Xunit;

namespace VRAtlas.Tests.Integration;

public class VRAtlasFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly Auth0ApiServer _auth0ApiServer;
    private readonly CloudflareApiServer _cloudflareApiServer;
    private readonly PostgreSqlContainer _mainDatabaseContainer;
    private readonly PostgreSqlContainer _quartzDatabaseContainer;
    private readonly IContainer _redisContainer;
    private readonly Auth0Options _auth0Options = new()
    {
        ClientId = nameof(VRAtlas),
        ClientSecret = nameof(VRAtlas),
        Audience = "https://test.vratlas.io",
        Domain = "https://test.vratlas.io"
    };

    public VRAtlasFactory()
    {
        _auth0ApiServer = new Auth0ApiServer();
        _cloudflareApiServer = new CloudflareApiServer();

        _mainDatabaseContainer = new PostgreSqlBuilder()
            .WithUsername(nameof(VRAtlas))
            .WithPassword(nameof(VRAtlas))
            .WithDatabase("vratlas_tests")
            .Build();

        _quartzDatabaseContainer = new PostgreSqlBuilder()
            .WithUsername(nameof(VRAtlas))
            .WithPassword(nameof(VRAtlas))
            .WithDatabase("vratlas_job_store_tests")
            .Build();

        _redisContainer = new ContainerBuilder()
            .WithName(Guid.NewGuid().ToString("D"))
            .WithImage("redis")
            .WithPortBinding(6379, true)
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
                { "Cloudflare:ApiKey", nameof(VRAtlas) },
                { "Cloudflare:ApiUrl", _cloudflareApiServer.Url },
                { "Auth0:Domain", _auth0Options.Domain },
                { "Auth0:Audience", _auth0Options.Audience },
                { "Auth0:ClientId", _auth0Options.ClientId },
                { "Auth0:ClientSecret", _auth0Options.ClientSecret },
                { "ConnectionStrings:Main", _mainDatabaseContainer.GetConnectionString() },
                { "ConnectionStrings:Quartz", _quartzDatabaseContainer.GetConnectionString() },
                { "ConnectionStrings:Redis", $"{_redisContainer.Hostname}:{_redisContainer.GetMappedPublicPort(6379)}" },
                { "VRAtlas:Salt", nameof(VRAtlas) },
                { "VRAtlas:CdnPath", "/" },
            });
        });

        builder.ConfigureTestServices(services =>
        {
            services.Configure<QuartzOptions>(options =>
            {
                options["quartz.scheduler.instanceName"] = "QuartzScheduler_Tests_" + _quartzDatabaseContainer.Name;
                options["quartz.dataSource.default.connectionString"] = _quartzDatabaseContainer.GetConnectionString();
            });
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = "Test";
                options.DefaultChallengeScheme = "Test";
            }).AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test", options => { });

            services.Remove(services.First(s => s.ImplementationType == typeof(HasPermissionHandler)));
        });
    }
    public async Task InitializeAsync()
    {
        _auth0ApiServer.Start();
        _cloudflareApiServer.Start();

        _auth0Options.Domain = _auth0ApiServer.Url;
        _auth0ApiServer.Configure("mycode", "vratlas.test|123456", _auth0Options.Audience, _auth0Options.ClientId, _auth0Options.ClientSecret, "https://redirect.vratlas.io/api/auth/callback");
        _cloudflareApiServer.Configure("abcdefghijklmnopqrstuvwxyz");

        await _redisContainer.StartAsync();
        await _mainDatabaseContainer.StartAsync();
        await _quartzDatabaseContainer.StartAsync();

        // Initialize the test job database tables
        await _quartzDatabaseContainer.ExecScriptAsync(File.ReadAllText("quartz.sql"));
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        _auth0ApiServer.Dispose();
        _cloudflareApiServer.Dispose();
        await _redisContainer.StopAsync();
        await _mainDatabaseContainer.StopAsync();
        await _quartzDatabaseContainer.StopAsync();
    }
}