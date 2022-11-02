using MicroElements.Swashbuckle.NodaTime;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NodaTime;
using NodaTime.Serialization.SystemTextJson;
using Serilog;
using Serilog.Events;
using StackExchange.Redis;
using System.IdentityModel.Tokens.Jwt;
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
var jwt = builder.Configuration.GetRequiredSection("Jwt").Get<JwtOptions>()!;

builder.Services
    .AddScoped<IAuthService, AuthService>()
    .AddScoped<IUserPermissionService, CachedUserPermissionService>()
    .AddScoped<IAuthorizationHandler, AtlasPermissionRequirementHandler>()
    .AddSingleton<IClock>(SystemClock.Instance)
    .AddSingleton<JwtSecurityTokenHandler>()
    .AddSingleton<IDiscordService, DiscordService>()
    .AddSingleton<IVariantCdnService, CloudflareVariantCdnService>()
    .AddSingleton<IAuthorizationMiddlewareResultHandler, AtlasAuthorizationMiddlewareResultHandler>()
    .AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(builder.Configuration.GetConnectionString("Redis") ?? string.Empty))
    .ConfigureRoleEndpoints()
    .ConfigureContextEndpoints()
    .ConfigureGroupEndpoints()
    .ConfigureEventEndpoints()
    .Configure<CloudflareOptions>(builder.Configuration.GetRequiredSection("Cloudflare"))
    .Configure<DiscordOptions>(builder.Configuration.GetRequiredSection("Discord"))
    .Configure<AzureOptions>(builder.Configuration.GetRequiredSection("Azure"))
    .Configure<JwtOptions>(builder.Configuration.GetRequiredSection("Jwt"))
    .Configure<JsonOptions>(options => options.SerializerOptions.ConfigureForNodaTime(DateTimeZoneProviders.Tzdb))
    .AddSwaggerGen(options => options.ConfigureForNodaTimeWithSystemTextJson())
    .AddHttpClient()
    .AddEndpointsApiExplorer()
    .AddCors(options => options.AddPolicy("_allowVRAOrigins", policy => policy
        .AllowAnyHeader().AllowAnyMethod().AllowCredentials()
        .WithOrigins(builder.Configuration.GetSection("CorsOrigins").Get<string[]>() ?? Array.Empty<string>())))
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
    .AddAuthentication(options => options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(jwt.Issuer, jwt.Audience, jwt.Key);

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
app.UseCors("_allowVRAOrigins");
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