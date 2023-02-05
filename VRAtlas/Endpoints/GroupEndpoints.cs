using FluentValidation;
using System.ComponentModel;
using System.Security.Claims;
using VRAtlas.Endpoints.Internal;
using VRAtlas.Endpoints.Validators;
using VRAtlas.Models;
using VRAtlas.Services;

namespace VRAtlas.Endpoints;

public class GroupEndpoints : IEndpointCollection
{
    [DisplayName("Create Group (Body)")]
    public record CreateGroupBody(string Name, string Description, Guid Icon, Guid Banner);

    [DisplayName("Update Group (Body)")]
    public record UpdateGroupBody(Guid Id, string Description, Guid Icon, Guid Banner);

    [DisplayName("Update Group Member (Body)")]
    public record MutateGroupMemberBody(Guid Id, Guid UserId, GroupMemberRole? Role);

    public static void BuildEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/groups");
        group.WithTags("Groups");

        group.MapGet("/{id:guid}", GetGroupById)
            .Produces<Group>(StatusCodes.Status200OK);

        group.MapGet("/user/{id:guid}", GetUserGroups)
            .Produces<IEnumerable<Group>>(StatusCodes.Status200OK)
            .WithTags("Groups");

        group.MapPost("/", CreateGroup)
            .Produces<Group>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden)
            .RequireAuthorization("create:groups")
            .AddValidationFilter<CreateGroupBody>();

        group.MapPut("/", UpdateGroup)
            .Produces<Group>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden)
            .RequireAuthorization("update:groups")
            .AddValidationFilter<UpdateGroupBody>();

        group.MapPut("/members/add", AddMemberToGroup)
            .Produces<Group>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden)
            .RequireAuthorization("update:groups")
            .AddValidationFilter<MutateGroupMemberBody>();

        group.MapPut("/members/remove", RemoveMemberFromGroup)
            .Produces<Group>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden)
            .RequireAuthorization("update:groups")
            .AddValidationFilter<MutateGroupMemberBody>();
    }

    public static void AddServices(IServiceCollection services)
    {
        services.AddScoped<IValidator<CreateGroupBody>, CreateGroupBodyValidator>();
        services.AddScoped<IValidator<UpdateGroupBody>, UpdateGroupBodyValidator>();
        services.AddScoped<IValidator<MutateGroupMemberBody>, MutateGroupMemberBodyValidator>();
    }

    private static async Task<IResult> GetGroupById(Guid id, IGroupService groupService)
    {
        var group = await groupService.GetGroupByIdAsync(id);
        if (group is null)
            return Results.NotFound();

        return Results.Ok(group);
    }

    private static async Task<IResult> GetUserGroups(Guid userId, IGroupService groupService)
    {
        var groups = await groupService.GetAllUserGroupsAsync(userId);
        return Results.Ok(groups);
    }

    private static async Task<IResult> CreateGroup(CreateGroupBody body, IGroupService groupService, IUserService userService, ClaimsPrincipal principal)
    {
        var user = await userService.GetUserAsync(principal);
        if (user is null)
            return Results.Unauthorized();

        var (name, description, icon, banner) = body;

        var group = await groupService.CreateGroupAsync(name, description, icon, banner, user.Id);

        return Results.Created($"/group/{group.Id}", group);
    }

    private static async Task<IResult> UpdateGroup(UpdateGroupBody body, IGroupService groupService, IUserService userService, ClaimsPrincipal principal)
    {
        var user = await userService.GetUserAsync(principal);
        if (user is null)
            return Results.Unauthorized();

        var (id, description, icon, banner) = body;
        var group = await groupService.ModifyGroupAsync(id, description, icon, banner);
        return Results.Ok(group);
    }

    private static async Task<IResult> AddMemberToGroup(MutateGroupMemberBody body, IGroupService groupService, IUserService userService, ClaimsPrincipal principal)
    {
        var user = await userService.GetUserAsync(principal);
        if (user is null)
            return Results.Unauthorized();

        var (id, userId, role) = body;
        var group = await groupService.AddGroupMemberAsync(id, userId, role ?? GroupMemberRole.Standard);
        return Results.Ok(group);
    }

    private static async Task<IResult> RemoveMemberFromGroup(MutateGroupMemberBody body, IGroupService groupService, IUserService userService, ClaimsPrincipal principal)
    {
        var user = await userService.GetUserAsync(principal);
        if (user is null)
            return Results.Unauthorized();

        var (id, userId, _) = body;
        var group = await groupService.RemoveGroupMemberAsync(id, userId);
        return Results.Ok(group);
    }
}