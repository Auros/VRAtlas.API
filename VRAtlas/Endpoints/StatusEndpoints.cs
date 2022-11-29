using VRAtlas.Endpoints.Internal;
using VRAtlas.Models;

namespace VRAtlas.Endpoints;

public class StatusEndpoints : IEndpointCollection
{
    public static void AddServices(IServiceCollection services)
    {
        // Unused | The status endpoints do not depend on any unique services
    }

    public static void BuildEndpoints(IEndpointRouteBuilder app)
    {
        app.MapGet("/status", GetStatus)
            .Produces<ApiStatus>(StatusCodes.Status200OK)
            .WithTags("Status");
    }

    private static IResult GetStatus()
    {
        ApiStatus status = new() { Status = "OK" };
        return Results.Ok(status);
    }
}