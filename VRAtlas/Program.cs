using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using NodaTime;
using Serilog;
using Serilog.Events;
using StackExchange.Redis;
using VRAtlas;
using VRAtlas.Authorization;
using VRAtlas.Endpoints;
using VRAtlas.Models.Options;
using VRAtlas.Services;
using VRAtlas.Services.Implementations;
using SystemClock = NodaTime.SystemClock;

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
    .AddScoped<IAuthService, AuthService>()
    .AddScoped<IUserPermissionService, CachedUserPermissionService>()
    .AddScoped<IAuthorizationHandler, AtlasPermissionRequirementHandler>()
    .AddSingleton<IClock>(SystemClock.Instance)
    .AddSingleton<IImageCdnService, CloudflareImageCdnService>()
    .AddSingleton<IAuthorizationMiddlewareResultHandler, AtlasAuthorizationMiddlewareResultHandler>()
    .AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(builder.Configuration.GetConnectionString("Redis") ?? string.Empty))
    .Configure<CloudflareOptions>(builder.Configuration.GetRequiredSection("Cloudflare"))
    .AddSwaggerGen()
    .AddHttpClient()
    .AddEndpointsApiExplorer()
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
    .AddAuthorization(options =>
    {

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

        // This event adds our custom claims to the user.
        options.Events.AddAtlasClaimEvent();
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

app.MapAuthEndpoints();
app.MapUserEndpoints();

// Seed the services with the necessary information required to run VR Atlas.
await app.SeedAtlas();

app.Run();

// This is used to expose the generated Program class to tests in the future.
public partial class Program { }