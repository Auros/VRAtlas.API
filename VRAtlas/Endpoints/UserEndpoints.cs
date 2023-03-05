using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using VRAtlas.Attributes;
using VRAtlas.Endpoints.Internal;
using VRAtlas.Endpoints.Validators;
using VRAtlas.Models;
using VRAtlas.Models.DTO;
using VRAtlas.Services;

namespace VRAtlas.Endpoints;

public class UserEndpoints : IEndpointCollection
{
    [VisualName("Update User (Body)")]
    public record UpdateUserBody(string Biography, IEnumerable<string> Links, NotificationInfoDTO Notifications, ProfileStatus ProfileStatus);

    [VisualName("Profile Metadata")]
    public record ProfileMetadata(int Followers, int Following);

    [VisualName("Paginated User Query")]
    public record PaginatedUserQuery(IEnumerable<UserDTO> Users, int? Next);

    [VisualName("Paginated Group Query")]
    public record PaginatedGroupQuery(IEnumerable<GroupDTO> Groups, int? Next);

    public static void BuildEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/users");
        group.WithTags("Users");

        group.MapGet("/@me", GetAuthUser)
            .Produces<UserDTO>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .RequireAuthorization();

        group.MapGet("/{id:guid}", GetUserById)
            .Produces<UserDTO>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        group.MapGet("/search", SearchForUsers)
            .Produces<IEnumerable<UserDTO>>(StatusCodes.Status200OK);

        group.MapPut("/@me", EditAuthUser)
            .Produces<UserDTO>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .RequireAuthorization()
            .AddValidationFilter<UpdateUserBody>();

        group.MapGet("/{id:guid}/profile", GetProfileMetadata)
            .Produces<ProfileMetadata>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized);

        group.MapGet("/{id:guid}/profile/followers", GetUserFollowers)
            .Produces<PaginatedUserQuery>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized);

        group.MapGet("/{id:guid}/profile/following", GetUserFollowing)
            .Produces<PaginatedUserQuery>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized);

        group.MapGet("/{id:guid}/profile/groups", GetUserGroups)
            .Produces<PaginatedGroupQuery>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized);
    }

    public static void AddServices(IServiceCollection services)
    {
        services.AddScoped<IValidator<UpdateUserBody>, UpdateUserBodyValidator>();
    }

    private static async Task<IResult> GetAuthUser(IUserService userService, ClaimsPrincipal principal)
    {
        var user = await userService.GetUserAsync(principal);
        if (user is null)
            return Results.Unauthorized();

        return Results.Ok(user.Map());
    }

    private static async Task<IResult> GetUserById(IUserService userService, Guid id, ClaimsPrincipal principal)
    {
        var user = await userService.GetUserAsync(id);
        if (user is null)
            return Results.NotFound();

        return Results.Ok(user.Map());
    }

    private static async Task<IResult> SearchForUsers(ClaimsPrincipal principal, IUserService userService, [FromQuery] string? query = null)
    {
        // If the search parameter has nothing.
        if (string.IsNullOrWhiteSpace(query))
            return Results.Ok(Array.Empty<UserDTO>());

        var users = await userService.GetUsersAsync(query);

        return Results.Ok(users.Map());
    }

    private static async Task<IResult> EditAuthUser(UpdateUserBody body, IUserService userService, ClaimsPrincipal principal)
    {
        var (bio, links, notif, profileStatus) = body;
        var user = await userService.EditUserAsync(principal, bio, links, profileStatus, new NotificationMetadata
        {
            AtThirtyMinutes = notif.AtThirtyMinutes,
            AtStart = notif.AtStart,
            AtOneHour = notif.AtOneHour,
            AtOneDay = notif.AtOneDay,
        });

        if (user is null)
            return Results.Unauthorized();

        return Results.Ok(user.Map());
    }

    private static async Task<IResult> GetProfileMetadata(Guid id, IUserService userService, IProfileService profileService, ClaimsPrincipal principal)
    {
        var targetUser = await userService.GetUserAsync(id);
        if (targetUser is null)
            return Results.NotFound();

        // Check if the user's profile info is private, reject the request if necessary.
        if (targetUser.ProfileStatus is ProfileStatus.Private)
        {
            var currentUser = await userService.GetUserAsync(principal);
            if (currentUser?.Id != targetUser.Id)
                return Results.Unauthorized();
        }

        var (followers, following) = await profileService.GetProfileMetadataAsync(id);
        return Results.Ok(new ProfileMetadata(followers, following));
    }

    private static async Task<IResult> GetUserFollowers(Guid id, IUserService userService, IProfileService profileService, ClaimsPrincipal principal, int? cursor = null)
    {
        var targetUser = await userService.GetUserAsync(id);
        if (targetUser is null)
            return Results.NotFound();

        // Check if the user's profile info is private, reject the request if necessary.
        if (targetUser.ProfileStatus is ProfileStatus.Private)
        {
            var currentUser = await userService.GetUserAsync(principal);
            if (currentUser?.Id != targetUser.Id)
                return Results.Unauthorized();
        }

        var (users, nextCursor) = await profileService.GetUserFollowersAsync(id, cursor, 25);
        return Results.Ok(new PaginatedUserQuery(users.Map(), nextCursor));
    }

    private static async Task<IResult> GetUserFollowing(Guid id, IUserService userService, IProfileService profileService, ClaimsPrincipal principal, int? cursor = null)
    {
        var targetUser = await userService.GetUserAsync(id);
        if (targetUser is null)
            return Results.NotFound();

        // Check if the user's profile info is private, reject the request if necessary.
        if (targetUser.ProfileStatus is ProfileStatus.Private)
        {
            var currentUser = await userService.GetUserAsync(principal);
            if (currentUser?.Id != targetUser.Id)
                return Results.Unauthorized();
        }

        var (users, nextCursor) = await profileService.GetUserFollowingAsync(id, cursor, 25);
        return Results.Ok(new PaginatedUserQuery(users.Map(), nextCursor));
    }

    private static async Task<IResult> GetUserGroups(Guid id, IUserService userService, IProfileService profileService, ClaimsPrincipal principal, int? cursor = null)
    {
        var targetUser = await userService.GetUserAsync(id);
        if (targetUser is null)
            return Results.NotFound();

        // Check if the user's profile info is private, reject the request if necessary.
        if (targetUser.ProfileStatus is ProfileStatus.Private)
        {
            var currentUser = await userService.GetUserAsync(principal);
            if (currentUser?.Id != targetUser.Id)
                return Results.Unauthorized();
        }

        var (groups, nextCursor) = await profileService.GetGroupFollowingAsync(id, cursor, 12);
        return Results.Ok(new PaginatedGroupQuery(groups.Map(), nextCursor));
    }
}