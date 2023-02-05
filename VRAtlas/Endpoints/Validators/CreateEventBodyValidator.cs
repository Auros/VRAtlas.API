using FluentValidation;
using VRAtlas.Services;

namespace VRAtlas.Endpoints.Validators;

public class CreateEventBodyValidator : AbstractValidator<EventEndpoints.CreateEventBody>
{
    private readonly IUserService _userService;
    private readonly IGroupService _groupService;
    private readonly IImageCdnService _imageCdnService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CreateEventBodyValidator(IUserService userService, IGroupService groupService, IImageCdnService imageCdnService, IHttpContextAccessor httpContextAccessor)
    {
        _userService = userService;
        _groupService = groupService;
        _imageCdnService = imageCdnService;
        _httpContextAccessor = httpContextAccessor;

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("An event name must be provided.");

        RuleFor(x => x.Group)
            .NotEmpty().WithMessage("A group must be provided.")
            .MustAsync(EnsureGroupExistsAsync).WithMessage("Group does not exist.")
            .MustAsync(EnsureUserCanCreateEventForGroup).WithMessage("Invalid group permissions.");

        RuleFor(x => x.Media)
            .NotEmpty().WithMessage("Invalid media resource id.")
            .MustAsync(EnsureValidImageAsync).WithMessage("Invalid icon image resource id.");
    }

    private Task<bool> EnsureGroupExistsAsync(Guid id, CancellationToken _)
    {
        return _groupService.GroupExistsAsync(id);
    }

    private Task<bool> EnsureUserCanCreateEventForGroup(Guid id, CancellationToken _)
    {
        return ValidationMethods.EnsureUserCanUpdateGroupAsync(id, _httpContextAccessor, _userService, _groupService);
    }

    private Task<bool> EnsureValidImageAsync(Guid resourceId, CancellationToken _)
    {
        return ValidationMethods.EnsureValidImageAsync(resourceId, _httpContextAccessor, _userService, _imageCdnService);
    }
}