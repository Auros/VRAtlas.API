using System.Net;
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

        var escape = Uri.EscapeDataString;
        var oauthUrl = $"/oauth/token?audience={escape(audience)}&client_id={escape(clientId)}&grant_type=client_credentials&client_secret={escape(clientSecret)}";
        Server.Given(Request.Create()
            .WithPath(oauthUrl)
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

        var codeUrl = $"/oauth/token?code={escape(code)}&redirect_uri={escape(redirectUri)}&client_id={escape(clientId)}&grant_type=authorization_code&client_secret={escape(clientSecret)}";
        Server.Given(Request.Create()
            .WithPath(codeUrl)
            .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(HttpStatusCode.OK)
                .WithHeader("Content-Type", "application/json")
                .WithBody("""
                {
                    "id_token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiJ2cmF0bGFzLnRlc3R8MTIzNDU2IiwibmFtZSI6IlZSQXRsYXMgVXNlciIsInBpY3R1cmUiOiJodHRwczovL3RoaXJkcGFydHkudnJhdGxhcy5jb20vaW1hZ2VzLzEyMzQ1Ni9hc2RmaGFzaGFzZGYvaW1hZ2UucG5nP3NpemU9NTEyIiwiaWF0IjoxNTE2MjM5MDIyLCJleHAiOjE3NjQ1NjUyMDB9.kU19IW5X1ZIY78v3TyuN8HA1H_E0hZY2fJE7Lzop1B4",
                    "access_token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiJ2cmF0bGFzLnRlc3R8MTIzNDU2IiwibmFtZSI6IlZSQXRsYXMgVXNlciIsImlhdCI6MTUxNjIzOTAyMiwiZXhwIjoxNzY0NTY1MjAwfQ.b0vwZkjjDqrwpmFMGwztIgZP1or37swFMWJj9jfGiGg",
                    "refresh_token": null,
                    "expires_in": 604800
                }
                """));
    }
}