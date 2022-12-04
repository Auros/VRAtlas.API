using System.Net;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;

namespace VRAtlas.Tests.Integration.Servers;

internal class CloudflareApiServer : ApiServer
{
    public void Configure(string accountHash)
    {
        Server.Given(Request.Create()
            .WithPath($"/client/v4/accounts/{accountHash}/images/v2/direct_upload")
            .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(HttpStatusCode.OK)
                .WithHeader("Content-Type", "application/json")
                .WithBody("""
                {
                    "result": {
                        "uploadURL": "https://cloudflare.vratlas.io/images/upload/asdfasdfasdfasdf"
                    }
                }
                """));

        Server.Given(Request.Create()
            .WithPath($"/client/v4/accounts/{accountHash}/images/v1")
            .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(HttpStatusCode.OK)
                .WithHeader("Content-Type", "application/json")
                .WithBody("""
                {
                    "result": {
                        "id": "0852c38d-8b20-414b-93c9-896bb1013fdd"
                    }
                }
                """));
    }
}