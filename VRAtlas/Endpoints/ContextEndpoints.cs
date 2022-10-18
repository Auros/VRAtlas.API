using VRAtlas.Models;

namespace VRAtlas.Endpoints;

public static class ContextEndpoints
{
    public static IEndpointRouteBuilder MapContextEndpoints(this IEndpointRouteBuilder builder)
    {
        builder.MapGet("/contexts", GetAllContexts).Produces<IEnumerable<Context>>(StatusCodes.Status200OK);
        builder.MapPost("/context/test", TestContextUpload);
        return builder;
    }

    internal static async Task<IResult> GetAllContexts()
    {
        await Task.Yield();
        return Results.Ok(Array.Empty<Context>());
    }

    internal static async Task<IResult> TestContextUpload(IFormFile file)
    {
        var name = file.FileName;

        await Task.Yield();

        return Results.Ok(name);
    }

    public class Test
    {
        public string Name { get; set; } = null!;
        public IFormFile File { get; set; } = null!;
    }
}