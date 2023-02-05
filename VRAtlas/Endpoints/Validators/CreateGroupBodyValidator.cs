using FluentValidation;
using VRAtlas.Services;

namespace VRAtlas.Endpoints.Validators;

public class CreateGroupBodyValidator : AbstractValidator<GroupEndpoints.CreateGroupBody>
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

        RuleFor(x => x.Description).NotNull().MaximumLength(1000);
        RuleFor(x => x.Icon).NotEmpty().MustAsync(EnsureValidImageAsync).WithMessage("Invalid icon image resource id.");
        RuleFor(x => x.Banner).NotEmpty().MustAsync(EnsureValidImageAsync).WithMessage("Invalid banner image resource id.");
        RuleFor(x => x.Name).NotEmpty().MustAsync(EnsureUniqueNameAsync).WithMessage("A group with that name already exists.");
    }

    private Task<bool> EnsureUniqueNameAsync(string name, CancellationToken _)
    {
        return _groupService.GroupExistsByNameAsync(name);
    }

    private Task<bool> EnsureValidImageAsync(Guid resourceId, CancellationToken _)
    {
        return ValidationMethods.EnsureValidImageAsync(resourceId, _httpContextAccessor, _userService, _imageCdnService);
    }
}