using LitJWT;
using LitJWT.Algorithms;
using MicroElements.Swashbuckle.NodaTime;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using NodaTime;
using NodaTime.Serialization.SystemTextJson;
using Quartz;
using Serilog;
using Serilog.Events;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using VRAtlas;
using VRAtlas.Authorization;
using VRAtlas.Endpoints.Internal;
using VRAtlas.Logging;
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

var auth0 = builder.Configuration.GetSection(Auth0Options.Name).Get<Auth0Options>()!;
var cloudflare = builder.Configuration.GetSection(CloudflareOptions.Name).Get<CloudflareOptions>()!;

// Service registration
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IEventService, EventService>();
builder.Services.AddSingleton<IAuthService, AuthService>();
builder.Services.AddScoped<IUserGrantService, UserGrantService>();
builder.Services.AddSingleton<IImageCdnService, CloudflareImageCdnService>();

// Jwt registration
builder.Services.AddSingleton<JwtEncoder>();
builder.Services.AddSingleton(services => new JwtDecoder(services.GetRequiredService<IJwtAlgorithm>()));
builder.Services.AddSingleton<IJwtAlgorithm>(services => new HS256Algorithm(Encoding.UTF8.GetBytes(services.GetRequiredService<IOptions<Auth0Options>>().Value.ClientSecret)));

// Core registration
builder.Services.AddSingleton<IClock>(SystemClock.Instance);
builder.Services.AddSingleton(typeof(IAtlasLogger<>), typeof(AtlasLogger<>));

// Option registration
builder.Services.AddOptions<Auth0Options>().BindConfiguration(Auth0Options.Name).ValidateDataAnnotations();
builder.Services.AddOptions<CloudflareOptions>().BindConfiguration(CloudflareOptions.Name).ValidateDataAnnotations();

// Other registration
builder.Services.AddVRAtlasEndpoints();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddDbContext<AtlasContext>((container, options) =>
{
    var connString = container.GetRequiredService<IConfiguration>().GetConnectionString("Main") ?? string.Empty;
    options.UseNpgsql(connString, npgsqlOptions => npgsqlOptions.UseNodaTime());
});
builder.Services.AddSwaggerGen(options =>
{
    options.ConfigureForNodaTimeWithSystemTextJson();
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
    options.AddScopes(auth0.Domain, new string[]
    {
        "edit:event"
    });
});
builder.Services.AddHttpClient("Auth0", (container, client) =>
{
    client.BaseAddress = new Uri(container.GetRequiredService<IOptions<Auth0Options>>().Value.Domain);
});
builder.Services.AddHttpClient("Cloudflare", (container, client) =>
{
    client.BaseAddress = cloudflare.ApiUrl;
    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", container.GetRequiredService<IOptions<CloudflareOptions>>().Value.ApiKey);
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
    });
});
builder.Services.AddQuartzServer(options =>
{
    options.WaitForJobsToComplete = true;
});
builder.Services.Configure<JsonOptions>(options =>
{
    options.SerializerOptions.ConfigureForNodaTime(DateTimeZoneProviders.Tzdb);
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();
app.UseAuthentication();
app.UseAuthorization();
app.UseVRAtlasEndpoints();

await app.SeedAtlas();

app.Run();

public partial class Program { /* Used as a class marker for tests */ }