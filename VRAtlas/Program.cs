using MicroElements.Swashbuckle.NodaTime;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.EntityFrameworkCore;
using NodaTime;
using NodaTime.Serialization.SystemTextJson;
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
    .AddSingleton<IVariantCdnService, CloudflareVariantCdnService>()
    .AddSingleton<IAuthorizationMiddlewareResultHandler, AtlasAuthorizationMiddlewareResultHandler>()
    .AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(builder.Configuration.GetConnectionString("Redis") ?? string.Empty))
    .ConfigureRoleEndpoints()
    .ConfigureContextEndpoints()
    .ConfigureGroupEndpoints()
    .ConfigureEventEndpoints()
    .Configure<CloudflareOptions>(builder.Configuration.GetRequiredSection("Cloudflare"))
    .Configure<AzureOptions>(builder.Configuration.GetRequiredSection("Azure"))
    .Configure<JsonOptions>(options => options.SerializerOptions.ConfigureForNodaTime(DateTimeZoneProviders.Tzdb))
    .AddSwaggerGen(options => options.ConfigureForNodaTimeWithSystemTextJson())
    .AddHttpClient()
    .AddEndpointsApiExplorer()
    .AddLogging(options =>
    {
        options.ClearProviders();
        options.AddSerilog(Log.Logger);
    })
    .AddOutputCache(options => options.DefaultExpirationTimeSpan = TimeSpan.FromDays(1))
    .AddDbContext<AtlasContext>(options =>
    {
        // If there is no connection string, we use an empty string to let UseNpgsql handle throwing the exception. 
        var connString = builder.Configuration.GetConnectionString("Database") ?? string.Empty;
        options.UseNpgsql(connString, npgsqlOptions => npgsqlOptions.UseNodaTime());
    })
    .AddAuthorization(options =>
    {
        options.AddPolicy("UploadUrl", o => o.AddRequirements(new AtlasPermissionRequirement(AtlasConstants.UserUploadUrl)));
        options.AddPolicy("CreateRole", o => o.AddRequirements(new AtlasPermissionRequirement(AtlasConstants.AdministratorRoleCreate)));
        options.AddPolicy("EditRole", o => o.AddRequirements(new AtlasPermissionRequirement(AtlasConstants.AdministratorRoleEdit)));
        options.AddPolicy("DeleteRole", o => o.AddRequirements(new AtlasPermissionRequirement(AtlasConstants.AdministratorRoleDelete)));
        options.AddPolicy("ManageContexts", o => o.AddRequirements(new AtlasPermissionRequirement(AtlasConstants.ManageContexts)));
        options.AddPolicy("CreateGroup", o => o.AddRequirements(new AtlasPermissionRequirement(AtlasConstants.CreateGroups)));
        options.AddPolicy("EditGroup", o => o.AddRequirements(new AtlasPermissionRequirement(AtlasConstants.EditGroups)));
        options.AddPolicy("CreateEvent", o => o.AddRequirements(new AtlasPermissionRequirement(AtlasConstants.UserEventCreate)));
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
app.MapRoleEndpoints();
app.MapContextEndpoints();
app.MapUploadEndpoints();
app.MapGroupEndpoints();
app.MapEventEndpoints();

// Seed the services with the necessary information required to run VR Atlas.
await app.SeedAtlas();

app.Run();

// This is used to expose the generated Program class to tests in the future.
public partial class Program { }