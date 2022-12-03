using LitJWT;
using LitJWT.Algorithms;
using MicroElements.Swashbuckle.NodaTime;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.EntityFrameworkCore;
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
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .MinimumLevel.Override(nameof(Microsoft), LogEventLevel.Debug)
    .Enrich.FromLogContext()
    .WriteTo.Async(options => options.Console())
    .CreateLogger();

var auth0 = builder.Configuration.GetSection(Auth0Options.Name).Get<Auth0Options>()!;
var cloudflare = builder.Configuration.GetSection(CloudflareOptions.Name).Get<CloudflareOptions>()!;

builder.Services.AddSingleton<JwtEncoder>();
builder.Services.AddSingleton(services => new JwtDecoder(services.GetRequiredService<IJwtAlgorithm>()));
builder.Services.AddSingleton<IJwtAlgorithm>(services => new HS256Algorithm(Encoding.UTF8.GetBytes(auth0.ClientSecret)));
builder.Services.AddSingleton<IAuthService, AuthService>();
builder.Services.AddSingleton<IClock>(SystemClock.Instance);
builder.Services.AddScoped<IUserGrantService, UserGrantService>();
builder.Services.AddSingleton(typeof(IAtlasLogger<>), typeof(AtlasLogger<>));
builder.Services.AddOptions<Auth0Options>().BindConfiguration(Auth0Options.Name).ValidateDataAnnotations();
builder.Services.AddOptions<CloudflareOptions>().BindConfiguration(CloudflareOptions.Name).ValidateDataAnnotations();
builder.Services.AddVRAtlasEndpoints();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddDbContext<AtlasContext>(options =>
{
    var connString = builder.Configuration.GetConnectionString("Main") ?? string.Empty;
    options.UseNpgsql(connString, npgsqlOptions => npgsqlOptions.UseNodaTime());
});
builder.Services.AddSwaggerGen(options =>
{
    options.ConfigureForNodaTimeWithSystemTextJson();
});
builder.Services.AddLogging(options =>
{
    options.ClearProviders();
    options.AddSerilog(Log.Logger);
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
builder.Services.AddHttpClient("Auth0", client =>
{
    client.BaseAddress = new Uri(auth0.Domain);
});
builder.Services.AddHttpClient("Cloudflare", client =>
{
    client.BaseAddress = cloudflare.ApiUrl;
    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", cloudflare.ApiKey);
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

app.Run();

public partial class Program { /* Used as a class marker for tests */ }