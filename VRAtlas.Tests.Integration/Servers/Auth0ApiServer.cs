using System.Net;
using WireMock.Matchers;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;

namespace VRAtlas.Tests.Integration.Servers;

internal class Auth0ApiServer : ApiServer
{
    public void Configure(string code, string userId, string audience, string clientId, string clientSecret, string redirectUri)
    {
        Server.Given(Request.Create()
            .WithPath($"/api/v2/users/{userId}/roles")
            .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(HttpStatusCode.OK)
                .WithHeader("Content-Type", "application/json")
                .WithBody("""
                {
                    "status": "ok"
                }
                """));

        Server.Given(Request.Create()
            .WithPath("/oauth/token")
            .WithHeader("Content-Type", new ExactMatcher("application/x-www-form-urlencoded"))
            .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(HttpStatusCode.OK)
                .WithHeader("Content-Type", "application/json")
                .WithBody("""
                {
                    "access_token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiJ2cmF0bGFzLnRlc3Quc2VydmVyfGFiY2RlIiwibmFtZSI6IklmIHlvdSdyZSByZWFkaW5nIHRoaXMsIGhlbGxvISIsImlhdCI6MTUxNjIzOTAyMiwiZXhwIjoxNzY0NTY1MjAwfQ.tTFSkiVLpAS77pNd9nSDd-EJ9x9ixcyoY77Z45_BWBs",
                    "expires_in": 604800
                }
                """));

        Server.Given(Request.Create()
            .WithPath("/oauth/token")
            .WithHeader("Content-Type", new ExactMatcher("application/x-www-form-urlencoded"))
            .WithHeader("Content-Length", new ExactMatcher("157"))
            .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(HttpStatusCode.OK)
                .WithHeader("Content-Type", "application/json")
                .WithBody($$"""
                {
                    "id_token": "{{TestConstants.ValidUserIdToken}}",
                    "access_token": "{{TestConstants.ValidUserAccessToken}}",
                    "refresh_token": null,
                    "expires_in": {{TestConstants.ValidUserTokenExpiration}}
                }
                """));
    }
}