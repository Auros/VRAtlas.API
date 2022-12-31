using FluentValidation;
using System.Security.Claims;
using VRAtlas.Endpoints.Internal;
using VRAtlas.Filters;
using VRAtlas.Models;
using VRAtlas.Services;

namespace VRAtlas.Endpoints;

public class GroupEndpoints : IEndpointCollection
{
    public static void BuildEndpoints(IEndpointRouteBuilder app)
    {
        app.MapGet("/group/{id:guid}", GetGroupById)
            .Produces<Group>(StatusCodes.Status200OK)
            .WithTags("Groups");

        app.MapGet("/groups/user/{id:guid}", GetUserGroups)
            .Produces<IEnumerable<Group>>(StatusCodes.Status200OK)
            .WithTags("Groups");

        app.MapPost("/group", CreateGroup)
            .Produces<Group>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden)
            .RequireAuthorization("create:groups")
            .AddEndpointFilter<ValidationFilter<CreateGroupBody>>()
            .WithTags("Groups");

        app.MapPut("/group", UpdateGroup)
            .Produces<Group>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden)
            .AddEndpointFilter<ValidationFilter<UpdateGroupBody>>()
            .RequireAuthorization("update:groups")
            .WithTags("Groups");

        app.MapPut("/group/members/add", AddMemberToGroup)
            .Produces<Group>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden)
            .AddEndpointFilter<ValidationFilter<MutateGroupMemberBody>>()
            .RequireAuthorization("update:groups")
            .WithTags("Groups");

        app.MapPut("/group/members/remove", RemoveMemberFromGroup)
            .Produces<Group>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden)
            .AddEndpointFilter<ValidationFilter<MutateGroupMemberBody>>()
            .RequireAuthorization("update:groups")
            .WithTags("Groups");
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

    #region Create Group

    public record CreateGroupBody(string Name, string Description, Guid Icon, Guid Banner);

    public class CreateGroupBodyValidator : AbstractValidator<CreateGroupBody>
    {
        private readonly IUserService _userService;
        private readonly IGroupService _groupService;
        private readonly IImageCdnService _imageCdnService;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CreateGroupBodyValidator(IUserService userService, IGroupService groupService, IImageCdnService imageCdnService, IHttpContextAccessor httpContextAccessor)
        {
            _userService = userService;
            _groupService = groupService;
            _imageCdnService = imageCdnService;
            _httpContextAccessor = httpContextAccessor;

            RuleFor(x => x.Name).NotEmpty().MustAsync(EnsureUniqueNameAsync).WithMessage("A group with that name already exists.");
            RuleFor(x => x.Description).NotNull().MaximumLength(1000);
            RuleFor(x => x.Icon).NotEmpty().MustAsync(EnsureValidImageAsync).WithMessage("Invalid icon image resource id.");
            RuleFor(x => x.Banner).NotEmpty().MustAsync(EnsureValidImageAsync).WithMessage("Invalid banner image resource id.");
        }

        private Task<bool> EnsureUniqueNameAsync(string name, CancellationToken _)
        {
            return _groupService.GroupExistsByNameAsync(name);
        }

        private Task<bool> EnsureValidImageAsync(Guid resourceId, CancellationToken _)
        {
            return GroupEndpoints.EnsureValidImageAsync(resourceId, _httpContextAccessor, _userService, _imageCdnService);
        }
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

    #endregion

    #region Update Group

    public record UpdateGroupBody(Guid Id, string Description, Guid Icon, Guid Banner);

    public class UpdateGroupBodyValidator : AbstractValidator<UpdateGroupBody>
    {
        private readonly IUserService _userService;
        private readonly IGroupService _groupService;
        private readonly IImageCdnService _imageCdnService;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public UpdateGroupBodyValidator(IUserService userService, IGroupService groupService, IImageCdnService imageCdnService, IHttpContextAccessor httpContextAccessor)
        {
            _userService = userService;
            _groupService = groupService;
            _imageCdnService = imageCdnService;
            _httpContextAccessor = httpContextAccessor;

            RuleFor(x => x.Id).NotEmpty().MustAsync(EnsureGroupExistsAsync).WithMessage("Group does not exist.").MustAsync(EnsureUserCanUpdateGroupAsync).WithMessage("Invalid group permissions.");
            RuleFor(x => x.Description).NotNull().MaximumLength(1000);
            RuleFor(x => x.Icon).NotEmpty().MustAsync(EnsureValidImageAsync).WithMessage("Invalid icon image resource id.");
            RuleFor(x => x.Banner).NotEmpty().MustAsync(EnsureValidImageAsync).WithMessage("Invalid banner image resource id.");
        }

        private Task<bool> EnsureGroupExistsAsync(Guid id, CancellationToken _)
        {
            return _groupService.GroupExistsAsync(id);
        }

        private Task<bool> EnsureUserCanUpdateGroupAsync(Guid id, CancellationToken _)
        {
            return GroupEndpoints.EnsureUserCanUpdateGroupAsync(id, _httpContextAccessor, _userService, _groupService);
        }

        private Task<bool> EnsureValidImageAsync(Guid resourceId, CancellationToken _)
        {
            return GroupEndpoints.EnsureValidImageAsync(resourceId, _httpContextAccessor, _userService, _imageCdnService);
        }
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

    #endregion

    #region Mutate Group Member

    public record MutateGroupMemberBody(Guid Id, Guid UserId, GroupMemberRole? Role);

    public class MutateGroupMemberBodyValidator : AbstractValidator<MutateGroupMemberBody>
    {
        private readonly IUserService _userService;
        private readonly IGroupService _groupService;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public MutateGroupMemberBodyValidator(IUserService userService, IGroupService groupService, IHttpContextAccessor httpContextAccessor)
        {
            _userService = userService;
            _groupService = groupService;
            _httpContextAccessor = httpContextAccessor;

            RuleFor(x => x.Id).NotEmpty().MustAsync(EnsureGroupExistsAsync).WithMessage("Group does not exist.").MustAsync(EnsureUserCanUpdateGroupAsync).WithMessage("Invalid group permissions.");
            RuleFor(x => x.UserId).NotEmpty().WithMessage("A user must be provided.");
            RuleFor(x => x.Role).NotEqual(GroupMemberRole.Owner).WithMessage("Cannot set a member's role to owner.");
        }

        private Task<bool> EnsureGroupExistsAsync(Guid id, CancellationToken _)
        {
            return _groupService.GroupExistsAsync(id);
        }

        private Task<bool> EnsureUserCanUpdateGroupAsync(Guid id, CancellationToken _)
        {
            return GroupEndpoints.EnsureUserCanUpdateGroupAsync(id, _httpContextAccessor, _userService, _groupService);
        }
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

    #endregion

    private static async Task<bool> EnsureValidImageAsync(Guid resourceId, IHttpContextAccessor httpContextAccessor, IUserService userService, IImageCdnService imageCdnService)
    {
        // Get the currently authenticated user.
        var principal = httpContextAccessor.HttpContext?.User;
        if (principal is null)
            return false;

        // Ensure that they're a user within our database.
        var user = await userService.GetUserAsync(principal);
        if (user is null)
            return false;

        // Validate that the image exists.
        return await imageCdnService.ValidateAsync(resourceId, user.Id);
    }

    private static async Task<bool> EnsureUserCanUpdateGroupAsync(Guid id, IHttpContextAccessor httpContextAccessor, IUserService userService, IGroupService groupService)
    {
        // Get the currently authenticated user.
        var principal = httpContextAccessor.HttpContext?.User;
        if (principal is null)
            return false;

        // Ensure that they're a user within our database.
        var user = await userService.GetUserAsync(principal);
        if (user is null)
            return false;

        // Ensure that the user has the valid permissions to modify the group.
        var role = await groupService.GetGroupMemberRoleAsync(id, user.Id);
        return role is GroupMemberRole.Owner || role is GroupMemberRole.Manager;
    }
}