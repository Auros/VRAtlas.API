using FluentValidation;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel;
using System.Security.Claims;
using VRAtlas.Endpoints.Internal;
using VRAtlas.Endpoints.Validators;
using VRAtlas.Models;
using VRAtlas.Services;

namespace VRAtlas.Endpoints;

public class NotificationEndpoints : IEndpointCollection
{
    [DisplayName("Paginated Notification Query")]
    public record PaginatedNotificationQuery(IEnumerable<Notification> Notifications, Guid? Next, int Unread);

    [DisplayName("Notification (Body)")]
    public record NotificationBody(Guid Id);

    public static void BuildEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/notifications");
        group.WithTags("Notifications");

        group.MapGet("/", GetNotifications)
            .Produces<PaginatedNotificationQuery>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .RequireAuthorization();

        group.MapGet("/@me", GetNotificationSettings)
            .Produces<NotificationMetadata>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .RequireAuthorization();

        group.MapPut("/read", ReadNotification)
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status401Unauthorized)
            .RequireAuthorization()
            .AddValidationFilter<NotificationBody>();
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
        return Results.Ok(new PaginatedNotificationQuery(notifs, outputCursor, unread));
    }

    public static async Task<IResult> GetNotificationSettings(IUserService userService, AtlasContext atlasContext, ClaimsPrincipal principal) // Only time we directly access the db context
    {
        var user = await userService.GetUserAsync(principal);
        if (user is null)
            return Results.Unauthorized();

        var settings = await atlasContext.Users.AsNoTracking().Where(u => u.Id == user.Id).Select(u => u.DefaultNotificationSettings).FirstAsync();
        return Results.Ok(settings);
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
}