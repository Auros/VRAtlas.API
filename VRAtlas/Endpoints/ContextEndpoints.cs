using VRAtlas.Models;

namespace VRAtlas.Endpoints;

public static class ContextEndpoints
{
    public static IEndpointRouteBuilder MapContextEndpoints(this IEndpointRouteBuilder builder)
    {
        builder.MapGet("/contexts", GetAllContexts).Produces<IEnumerable<Context>>(StatusCodes.Status200OK);
        return builder;
    }

    internal static async Task<IResult> GetAllContexts()
    {
        await Task.Yield();
        return Results.Ok(Array.Empty<Context>());
    }
}