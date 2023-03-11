using FluentValidation;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using VRAtlas.Attributes;
using VRAtlas.Endpoints.Internal;
using VRAtlas.Endpoints.Validators;
using VRAtlas.Models.DTO;
using VRAtlas.Options;
using VRAtlas.Services;

namespace VRAtlas.Endpoints;

public class NotificationEndpoints : IEndpointCollection
{
    [VisualName("Paginated Notification Query")]
    public record PaginatedNotificationQuery(IEnumerable<NotificationDTO> Notifications, Guid? Next, int Unread);

    [VisualName("Notification (Body)")]
    public record NotificationBody(Guid Id);

    [VisualName("Web Push Subscription (Body)")]
    public record WebPushSubscriptionBody(Uri Endpoint, IDictionary<string, string> Keys);

    public static void BuildEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/notifications");
        group.WithTags("Notifications");

        group.MapGet("/", GetNotifications)
            .Produces<PaginatedNotificationQuery>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .RequireAuthorization();

        group.MapGet("/@me", GetNotificationSettings)
            .Produces<NotificationInfoDTO>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .RequireAuthorization();

        group.MapPut("/read", ReadNotification)
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status401Unauthorized)
            .RequireAuthorization()
            .AddValidationFilter<NotificationBody>();

        // Only enable these endpoints if WebPush is configured
        if (app.ServiceProvider.GetRequiredService<IConfiguration>().GetSection(WebPushOptions.Name).Exists())
        {
            group.MapPost("/web", RegisterWebPushNotification)
                .Produces(StatusCodes.Status204NoContent)
                .Produces(StatusCodes.Status401Unauthorized)
                .RequireAuthorization();

            group.MapDelete("/web", UnregisterWebPushNotification)
                .Produces(StatusCodes.Status204NoContent)
                .Produces(StatusCodes.Status401Unauthorized)
                .RequireAuthorization();
        }
    }

    public static void AddServices(IServiceCollection services)
    {
        services.AddScoped<IValidator<NotificationBody>, NotificationBodyValidator>();
    }

    public static async Task<IResult> GetNotifications(IUserService userService, INotificationService notificationService, ClaimsPrincipal principal, Guid? cursor = null, bool? readOnly = null, int size = 5)
    {
        var user = await userService.GetUserAsync(principal);
        if (user is null)
            return Results.Unauthorized();

        var (notifs, outputCursor, unread) = await notificationService.QueryNotificationsAsync(user.Id, cursor, readOnly, size);
        return Results.Ok(new PaginatedNotificationQuery(notifs.Map(), outputCursor, unread));
    }

    public static async Task<IResult> GetNotificationSettings(IUserService userService, AtlasContext atlasContext, ClaimsPrincipal principal) // Only time we directly access the db context
    {
        var user = await userService.GetUserAsync(principal);
        if (user is null)
            return Results.Unauthorized();

        var settings = await atlasContext.Users.AsNoTracking().Where(u => u.Id == user.Id).Select(u => u.DefaultNotificationSettings).FirstAsync();
        return Results.Ok(settings!.Map());
    }

    public static async Task<IResult> ReadNotification(NotificationBody body, IUserService userService, INotificationService notificationService, ClaimsPrincipal principal)
    {
        var user = await userService.GetUserAsync(principal);
        if (user is null)
            return Results.Unauthorized();

        var success = await notificationService.MarkAsReadAsync(body.Id);
        if (!success)
            return Results.NotFound();

        return Results.NoContent();
    }

    public static async Task<IResult> RegisterWebPushNotification(WebPushSubscriptionBody body, IUserService userService, IPushNotificationService pushNotificationService, ClaimsPrincipal principal)
    {
        var user = await userService.GetUserAsync(principal);
        if (user is null)
            return Results.Unauthorized();

        var (endpoint, keys) = body;
        await pushNotificationService.SubscribeAsync(user.Id, endpoint.ToString(), keys["p256dh"], keys["auth"]); 
        return Results.NoContent();
    }

    public static async Task<IResult> UnregisterWebPushNotification(string endpoint, IPushNotificationService pushNotificationService)
    {
        var result = await pushNotificationService.UnsubscribeAsync(endpoint);
        return result ? Results.NoContent() : Results.NotFound();
    }
}