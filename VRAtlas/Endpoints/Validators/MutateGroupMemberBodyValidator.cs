using FluentValidation;
using VRAtlas.Models;
using VRAtlas.Services;

namespace VRAtlas.Endpoints.Validators;

public class MutateGroupMemberBodyValidator : AbstractValidator<GroupEndpoints.MutateGroupMemberBody>
{
    private readonly IUserService _userService;
    private readonly IGroupService _groupService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public MutateGroupMemberBodyValidator(IUserService userService, IGroupService groupService, IHttpContextAccessor httpContextAccessor)
    {
        _userService = userService;
        _groupService = groupService;
        _httpContextAccessor = httpContextAccessor;

        RuleFor(x => x.UserId).NotEmpty().WithMessage("A user must be provided.");
        RuleFor(x => x.Role).NotEqual(GroupMemberRole.Owner).WithMessage("Cannot set a member's role to owner.");
        RuleFor(x => x.Id).NotEmpty().MustAsync(EnsureGroupExistsAsync).WithMessage("Group does not exist.").MustAsync(EnsureUserCanUpdateGroupAsync).WithMessage("Invalid group permissions.");
    }

    private Task<bool> EnsureGroupExistsAsync(Guid id, CancellationToken _)
    {
        return _groupService.GroupExistsAsync(id);
    }

    private Task<bool> EnsureUserCanUpdateGroupAsync(Guid id, CancellationToken _)
    {
        return ValidationMethods.EnsureUserCanUpdateGroupAsync(id, _httpContextAccessor, _userService, _groupService);
    }
}