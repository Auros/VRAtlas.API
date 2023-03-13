using MessagePipe;
using Microsoft.AspNetCore.OutputCaching;
using VRAtlas.Endpoints.Internal;
using VRAtlas.Events;
using VRAtlas.Services;

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

        group.MapGet("/events/reschedule/{id:guid}", async (Guid id, IEventService eventService, IPublisher<EventScheduledEvent> publisher) =>
        {
            var atlasEvent = await eventService.GetEventByIdAsync(id);
            if (atlasEvent is null)
                return Results.NotFound(new { Error = "Not Found" });

            publisher.Publish(new EventScheduledEvent(id));
            return Results.Ok(new { Message = "OK!" });
        }).ExcludeFromDescription().RequireAuthorization("admin:reschedule");
    }
}