using System.Net.Http.Headers;

namespace VRAtlas.Tests.Integration;

internal static class TestConstants
{

    public const string ValidUserName = "VRAtlas User";

    public const string ValidAuth0Code = "myvalidcode";

    public const string ValidUserSocialId = "vratlas.test|123456";

    public const string ValidRogueAccessToken = "valid.rogue-token";

    public const string ValidAuth0RedirectUrl = "https://redirect.vratlas.io/api/auth/callback";

    public const string ValidUserAccessToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiJ2cmF0bGFzLnRlc3R8MTIzNDU2IiwibmFtZSI6IlZSQXRsYXMgVXNlciIsImlhdCI6MTUxNjIzOTAyMiwiZXhwIjoxNzY0NTY1MjAwLCJpc3MiOiJodHRwczovL3Rlc3QudnJhdGxhcy5pbyIsImF1ZCI6WyJodHRwczovL3Rlc3QudnJhdGxhcy5pbyJdfQ.r6nXFIzaPHSzgkpAjocamcJyAXpum5BYfsoi8FiLeEE";

    public const string ValidUserIdToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiJ2cmF0bGFzLnRlc3R8MTIzNDU2IiwibmFtZSI6IlZSQXRsYXMgVXNlciIsInBpY3R1cmUiOiJodHRwczovL3RoaXJkcGFydHkudnJhdGxhcy5jb20vaW1hZ2VzLzEyMzQ1Ni9hc2RmaGFzaGFzZGYvaW1hZ2UucG5nP3NpemU9NTEyIiwiaWF0IjoxNTE2MjM5MDIyLCJleHAiOjE3NjQ1NjUyMDB9.kU19IW5X1ZIY78v3TyuN8HA1H_E0hZY2fJE7Lzop1B4";

    public const int ValidUserTokenExpiration = 604800;

    public static async Task<string> LoginDefaultTestUser(this HttpClient httpClient)
    {
        var escape = Uri.EscapeDataString;
        var code = TestConstants.ValidAuth0Code;
        var redirectUri = TestConstants.ValidAuth0RedirectUrl;
        var validAccessToken = TestConstants.ValidUserAccessToken;

        var msg = new HttpRequestMessage
        {
            RequestUri = new Uri("user/@me", UriKind.Relative),
            Method = HttpMethod.Get
        };
        msg.Headers.Authorization = new AuthenticationHeaderValue("Test", validAccessToken);

        using var _ = await httpClient.GetAsync($"auth/token?code={escape(code)}&redirectUri={redirectUri}");

        return validAccessToken;
    }

    public static HttpRequestMessage CreateHttpMessage(string uri, string? accessToken = null, HttpMethod? method = null)
    {
        method ??= HttpMethod.Get;
        var msg = new HttpRequestMessage
        {
            Method = method,
            RequestUri = new Uri(uri, UriKind.Relative)
        };

        if (accessToken is not null)
            msg.Headers.Authorization = new AuthenticationHeaderValue("Test", accessToken);

        return msg;
    }
}