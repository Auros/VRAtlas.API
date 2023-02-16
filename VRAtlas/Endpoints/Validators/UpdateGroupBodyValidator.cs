using FluentValidation;
using VRAtlas.Services;

namespace VRAtlas.Endpoints.Validators;

public class UpdateGroupBodyValidator : AbstractValidator<GroupEndpoints.UpdateGroupBody>
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

        RuleFor(x => x.Description)
            .NotNull()
            .MaximumLength(1000);

        RuleFor(x => x.Icon)
            .MustAsync(EnsureValidImageAsync).WithMessage("Invalid icon image resource id.");

        RuleFor(x => x.Banner)
            .MustAsync(EnsureValidImageAsync).WithMessage("Invalid banner image resource id.");

        RuleFor(x => x.Id)
            .NotEmpty()
            .MustAsync(EnsureGroupExistsAsync).WithMessage("Group does not exist.")
            .MustAsync(EnsureUserCanUpdateGroupAsync).WithMessage("Invalid group permissions.");
    }

    private Task<bool> EnsureGroupExistsAsync(Guid id, CancellationToken _)
    {
        return _groupService.GroupExistsAsync(id);
    }

    private Task<bool> EnsureUserCanUpdateGroupAsync(Guid id, CancellationToken _)
    {
        return ValidationMethods.EnsureUserCanUpdateGroupAsync(id, _httpContextAccessor, _userService, _groupService);
    }

    private Task<bool> EnsureValidImageAsync(Guid? resourceId, CancellationToken _)
    {
        // Image is OK if it's not being updated
        if (!resourceId.HasValue)
            return Task.FromResult(true);

        return ValidationMethods.EnsureValidImageAsync(resourceId.GetValueOrDefault(), _httpContextAccessor, _userService, _imageCdnService);
    }
}