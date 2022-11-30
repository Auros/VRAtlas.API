using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using NodaTime;
using System.Security.Claims;
using VRAtlas.Authorization;
using VRAtlas.Endpoints.Internal;
using VRAtlas.Logging;
using VRAtlas.Options;

var builder = WebApplication.CreateBuilder(args);
var auth0 = builder.Configuration.GetSection(Auth0Options.Name).Get<Auth0Options>()!;

builder.Services.AddSingleton<IClock>(SystemClock.Instance);
builder.Services.AddSingleton(typeof(IAtlasLogger<>), typeof(AtlasLogger<>));
builder.Services.AddSwaggerGen();
builder.Services.AddVRAtlasEndpoints();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOptions<Auth0Options>().BindConfiguration(Auth0Options.Name).ValidateDataAnnotations();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = auth0.Domain;
        options.Audience = auth0.Audience;
        options.TokenValidationParameters = new TokenValidationParameters { NameClaimType = ClaimTypes.NameIdentifier };
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

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();
app.UseAuthentication();
app.UseAuthorization();
app.UseVRAtlasEndpoints();

app.Run();

public partial class Program { /* Used as a class marker for tests */ }