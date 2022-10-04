using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using NodaTime;
using Serilog;
using Serilog.Events;
using StackExchange.Redis;
using VRAtlas;
using VRAtlas.Endpoints;
using VRAtlas.Models.Options;
using VRAtlas.Services;
using VRAtlas.Services.Implementations;

var builder = WebApplication.CreateBuilder(args);

// Setup our logger with Serilog
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .MinimumLevel.Override(nameof(Microsoft), LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo.Async(options => options.Console())
    .CreateLogger();

var discord = builder.Configuration.GetRequiredSection("Auth:Providers:Discord");

builder.Services
    .AddSwaggerGen()
    .AddHttpClient()
    .AddAuthorization()
    .AddEndpointsApiExplorer()
    .AddScoped<IAuthService, AuthService>()
    .AddSingleton<IClock>(SystemClock.Instance)
    .AddSingleton<IImageCdnService, CloudflareImageCdnService>()
    .AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(builder.Configuration.GetConnectionString("Redis") ?? string.Empty))
    .Configure<CloudflareOptions>(builder.Configuration.GetRequiredSection("Cloudflare"))
    .AddLogging(options =>
    {
        options.ClearProviders();
        options.AddSerilog(Log.Logger);
    })
    .AddOutputCache(options =>
    {
        options.DefaultExpirationTimeSpan = TimeSpan.FromDays(1);
    })
    .AddDbContext<AtlasContext>(options =>
    {
        // If there is no connection string, we use an empty string to let UseNpgsql handle throwing the exception. 
        var connString = builder.Configuration.GetConnectionString("Database") ?? string.Empty;
        options.UseNpgsql(connString, npgsqlOptions =>
        {
            npgsqlOptions.UseNodaTime();
        });
    })
    .AddAuthentication(options => options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        // Disables login redirect on API endpoints
        options.Events.OnRedirectToLogin = ctx =>
        {
            ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
            ctx.Response.WriteAsJsonAsync(new { error = "Unauthorized" });
            return Task.CompletedTask;
        };
    })
    .AddDiscord(options =>
    {
        options.Scope.Add("email");
        options.ClientId = discord.GetRequiredSection("ClientId").Value!;
        options.ClientSecret = discord.GetRequiredSection("ClientSecret").Value!;
    });

// ------------------------
// Middleware configuration
// ------------------------

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseOutputCache();
app.UseAuthentication();
app.UseAuthorization();

app
    .MapAuthEndpoints()
    .MapUserEndpoints();

/* Database Migration if necessary */
var scope = app.Services.CreateAsyncScope();
var migrationLogger = scope.ServiceProvider.GetRequiredService<ILogger<AtlasContext>>();
var mercuryContext = scope.ServiceProvider.GetRequiredService<AtlasContext>();

migrationLogger.LogInformation("Attempting to perform database migration");
await mercuryContext.Database.MigrateAsync();
migrationLogger.LogInformation("Disposing migration container");
await scope.DisposeAsync();

app.Run();

// This is used to expose the generated Program class to tests in the future.
public partial class Program { }