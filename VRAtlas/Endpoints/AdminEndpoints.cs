using Microsoft.AspNetCore.OutputCaching;
using VRAtlas.Endpoints.Internal;

namespace VRAtlas.Endpoints;

public class AdminEndpoints : IEndpointCollection
{
    public static void BuildEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/admin");
        group.WithTags("Admin");

        group.MapGet("/clear/{tag}", async (string tag, IOutputCacheStore cache, CancellationToken token) =>
        {
            await cache.EvictByTagAsync(tag, token);
            return Results.Ok(new { Message = "Cleared" });
        }).ExcludeFromDescription().RequireAuthorization("admin:clear");
    }
}