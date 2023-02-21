using FluentValidation;
using VRAtlas.Models;
using VRAtlas.Services;

namespace VRAtlas.Endpoints.Validators;

public class UpdateEventBodyValidator : AbstractValidator<EventEndpoints.UpdateEventBody>
{
    private readonly IUserService _userService;
    private readonly IEventService _eventService;
    private readonly IGroupService _groupService;
    private readonly IImageCdnService _imageCdnService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public UpdateEventBodyValidator(IUserService userService, IEventService eventService, IGroupService groupService, IImageCdnService imageCdnService, IHttpContextAccessor httpContextAccessor)
    {
        _userService = userService;
        _eventService = eventService;
        _groupService = groupService;
        _imageCdnService = imageCdnService;
        _httpContextAccessor = httpContextAccessor;

        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("An event id must be provided.")
            .MustAsync(EnsureEventExistsAsync).WithMessage("Event does not exist.")
            .MustAsync(EnsureUserCanUpdateEventAsync).WithMessage("Lacking group permissions to update this event.")
            .MustAsync(EnsureEventIsWritableAsync).WithMessage("Event has been concluded or canceled, it cannot be edited!");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("An event name must be provided.");

        RuleFor(x => x.Description)
            .NotNull().WithMessage("A description must be provided.")
            .MaximumLength(2000).WithMessage("Description is too long, maximum length is 2000 characters.");

        RuleFor(x => x.Media)
            .MustAsync(EnsureValidImageAsync).WithMessage("Invalid icon image resource id.");

        RuleFor(x => x.Tags)
            .Must(t => t.Length <= 50).WithMessage("Cannot have more than 50 tags.")
            .NotNull().WithMessage("Tags property must be provided.");

        RuleFor(x => x.Stars)
            .NotNull().WithMessage("Stars property must be provided.");
    }

    private Task<bool> EnsureEventExistsAsync(Guid id, CancellationToken _)
    {
        return _eventService.EventExistsAsync(id);
    }

    private async Task<bool> EnsureEventIsWritableAsync(Guid id, CancellationToken _)
    {
        return (await _eventService.GetEventStatusAsync(id)) is EventStatus.Unlisted or EventStatus.Announced or EventStatus.Started;
    }


    private Task<bool> EnsureValidImageAsync(Guid? resourceId, CancellationToken _)
    {
        // Image is OK if it's not being updated
        if (!resourceId.HasValue)
            return Task.FromResult(true);

        return ValidationMethods.EnsureValidImageAsync(resourceId.Value, _httpContextAccessor, _userService, _imageCdnService);
    }

    private async Task<bool> EnsureUserCanUpdateEventAsync(Guid id, CancellationToken _)
    {
        var groupId = await _eventService.GetEventGroupIdAsync(id);
        if (groupId == default)
            return false;

        return await ValidationMethods.EnsureUserCanUpdateGroupAsync(groupId, _httpContextAccessor, _userService, _groupService);
    }
}