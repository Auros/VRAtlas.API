using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace VRAtlas.Tests.Integration;

internal class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public TestAuthHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder, ISystemClock clock) : base(options, logger, encoder, clock)
    {

    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var values = Context.Request.Headers.Authorization;

        if (values.Count == 0)
            return Task.FromResult(AuthenticateResult.NoResult());

        var authName = values.ToString().Split(" ").Last();

        if (authName != TestConstants.ValidUserAccessToken && authName != TestConstants.ValidRogueAccessToken)
            return Task.FromResult(AuthenticateResult.NoResult());

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, authName == TestConstants.ValidUserAccessToken ? TestConstants.ValidUserSocialId : "rogue.social.id"),
        };

        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, "Test");
        var result = AuthenticateResult.Success(ticket);
        return Task.FromResult(result);
    }
}