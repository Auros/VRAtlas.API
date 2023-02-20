using LitJWT;
using LitJWT.Algorithms;
using MessagePipe;
using MicroElements.Swashbuckle.NodaTime;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using NodaTime;
using NodaTime.Serialization.SystemTextJson;
using Quartz;
using Serilog;
using Serilog.Events;
using System.ComponentModel;
using System.Net.Http.Headers;
using System.Reflection;
using System.Security.Claims;
using System.Text;
using VRAtlas;
using VRAtlas.Authorization;
using VRAtlas.Converters;
using VRAtlas.Core;
using VRAtlas.Endpoints.Internal;
using VRAtlas.Events;
using VRAtlas.Jobs;
using VRAtlas.Listeners;
using VRAtlas.Logging;
using VRAtlas.Models;
using VRAtlas.Options;
using VRAtlas.Services;

var builder = WebApplication.CreateBuilder(args);

// Setup our logger with Serilog
var logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override(nameof(Microsoft), LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo.Async(options => options.Console())
    .CreateLogger();
Log.Logger = logger;

var auth0 = builder.Configuration.GetSection(Auth0Options.Name).Get<Auth0Options>() ?? new Auth0Options { Audience = string.Empty, ClientId = string.Empty, ClientSecret = string.Empty, Domain = string.Empty };
var cloudflare = builder.Configuration.GetSection(CloudflareOptions.Name).Get<CloudflareOptions>() ?? new CloudflareOptions { ApiKey = string.Empty, ApiUrl = new Uri("http://localhost") };

// Service registration
builder.Services.AddScoped<ITagService, TagService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IEventService, EventService>();
builder.Services.AddScoped<IGroupService, GroupService>();
builder.Services.AddScoped<IFollowService, FollowService>();
builder.Services.AddScoped<IUserGrantService, UserGrantService>();

builder.Services.AddSingleton<IAuthService, AuthService>();
builder.Services.AddSingleton<IImageCdnService, CloudflareImageCdnService>();

// Event registration
builder.Services.AddScopedEventListener<EventStatusUpdatedEvent, EventAnnouncementListener>();

// Jwt registration
builder.Services.AddSingleton<JwtEncoder>();
builder.Services.AddSingleton(services => new JwtDecoder(services.GetRequiredService<IJwtAlgorithm>()));
builder.Services.AddSingleton<IJwtAlgorithm>(services => new HS256Algorithm(Encoding.UTF8.GetBytes(services.GetRequiredService<IOptions<Auth0Options>>().Value.ClientSecret)));

// Core registration
builder.Services.AddMessagePipe();
builder.Services.AddSingleton<IClock>(SystemClock.Instance);
builder.Services.AddSingleton<IAuthorizationHandler, HasPermissionHandler>();
builder.Services.AddSingleton(typeof(IAtlasLogger<>), typeof(AtlasLogger<>));

// Option registration
builder.Services.AddOptions<Auth0Options>().BindConfiguration(Auth0Options.Name).ValidateDataAnnotations();
builder.Services.AddOptions<CloudflareOptions>().BindConfiguration(CloudflareOptions.Name).ValidateDataAnnotations();

// Other registration
builder.Services.AddVRAtlasEndpoints();
builder.Services.AddHttpContextAccessor();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddDbContext<AtlasContext>((container, options) =>
{
    var connString = container.GetRequiredService<IConfiguration>().GetConnectionString("Main") ?? string.Empty;
    options.UseNpgsql(connString, npgsqlOptions => npgsqlOptions.UseNodaTime());
});
builder.Services.AddSwaggerGen(options =>
{
    // TODO: Move into separate file
    options.CustomSchemaIds(selector =>
    {
        // Since we're using the DisplayName attribute, we need to map the enums separately.
        // TODO: Switch to using a custom attribute that supports enums
        if (selector.IsEnum)
        {
            if (selector == typeof(EventStatus))
                return "Event Status";

            if (selector == typeof(EventStarStatus))
                return "Event Star Status";

            if (selector == typeof(GroupMemberRole))
                return "Group Member Role";
        }
        return selector.GetCustomAttributes<DisplayNameAttribute>().FirstOrDefault()?.DisplayName ?? selector.Name;
    });
    options.ConfigureForNodaTimeWithSystemTextJson();

    OpenApiSecurityScheme securityScheme = new()
    {
        BearerFormat = "JWT",
        Name = "JWT Authentication",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = JwtBearerDefaults.AuthenticationScheme,
        Description = "Specify JWT Bearer for authenticated requests",
        Reference = new OpenApiReference
        {
            Id = JwtBearerDefaults.AuthenticationScheme,
            Type = ReferenceType.SecurityScheme
        }
    };

    options.AddSecurityDefinition(securityScheme.Reference.Id, securityScheme);
    options.AddSecurityRequirement(new OpenApiSecurityRequirement { { securityScheme, Array.Empty<string>() } });
});
builder.Services.AddLogging(options =>
{
    options.ClearProviders();
    options.AddSerilog(logger);
});
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{   
    options.Authority = auth0.Domain;
    options.Audience = auth0.Audience;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        NameClaimType = ClaimTypes.NameIdentifier
    };
});
builder.Services.AddAuthorization(options =>
{
    options.AddPermissions(auth0.Domain, new string[]
    {
        "create:upload_url",
        "create:groups",
        "update:groups",
        "create:events",
        "update:events",

    });
});
builder.Services.AddHttpClient("Auth0", (container, client) =>
{
    client.BaseAddress = new Uri(container.GetRequiredService<IOptions<Auth0Options>>().Value.Domain);
});
builder.Services.AddHttpClient("Cloudflare", (container, client) =>
{
    var options = container.GetRequiredService<IOptions<CloudflareOptions>>().Value;
    client.BaseAddress = options.ApiUrl;
    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", options.ApiKey);
});
builder.Services.AddQuartz(options =>
{
    options.UseMicrosoftDependencyInjectionJobFactory();
    options.UseDefaultThreadPool(tpo => tpo.MaxConcurrency = 10);
    options.UsePersistentStore(store =>
    {
        var quartzConnString = builder.Configuration.GetConnectionString("Quartz") ?? string.Empty;
        store.UsePostgres(quartzConnString);
        store.UseJsonSerializer();

        options.AddJob<EventStartingJob>(jc =>
        {
            jc.StoreDurably();
            jc.WithIdentity(EventStartingJob.Key);
            jc.WithDescription("Activates when an event is supposed to start.");
        });
        options.AddJob<EventEndingJob>(jc =>
        {
            jc.StoreDurably();
            jc.WithIdentity(EventEndingJob.Key);
            jc.WithDescription("Activates when an event is supposed to end.");
        });
        options.AddJob<EventReminderJob>(jc =>
        {
            jc.StoreDurably();
            jc.WithIdentity(EventReminderJob.Key);
            jc.WithDescription("Activates when an event reminder point gets hit.");
        });
    });
});
builder.Services.AddQuartzServer(options =>
{
    options.WaitForJobsToComplete = true;
});
builder.Services.Configure<JsonOptions>(options =>
{
    options.SerializerOptions.Converters.Add(new WritableTagModelJsonConverter());
    options.SerializerOptions.ConfigureForNodaTime(DateTimeZoneProviders.Tzdb);
});

var app = builder.Build();

app.UseSwagger(options =>
{
    // When swagger is viewed through a reverse proxy, make sure to respect any added prefixes on the proxy.
    options.PreSerializeFilters.Add((swagger, req) =>
    {
        if (!req.Headers.TryGetValue("X-Forwarded-Prefix", out var value))
            return;
        
        swagger.Servers = new List<OpenApiServer> { new OpenApiServer { Url = value } };
    });
});
app.UseSwaggerUI();
app.UseAuthentication();
app.UseAuthorization();
app.UseVRAtlasEndpoints();

await app.SeedAtlas();

app.Run();

public partial class Program { /* Used as a class marker for tests */ }