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
    }

    public static void AddServices(IServiceCollection services)
    {
        services.AddScoped<IValidator<CreateGroupBody>, CreateGroupBodyValidator>();
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

        private async Task<bool> EnsureValidImageAsync(Guid resourceId, CancellationToken _)
        {
            var principal = _httpContextAccessor.HttpContext?.User;
            if (principal is null)
                return false;

            var user = await _userService.GetUserAsync(principal);
            if (user is null)
                return false;

            return await _imageCdnService.ValidateAsync(resourceId, user.Id);
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
}