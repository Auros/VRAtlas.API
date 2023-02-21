using FluentValidation;
using NodaTime;
using VRAtlas.Services;

namespace VRAtlas.Endpoints.Validators;

public class ScheduleEventBodyValidator : AbstractValidator<EventEndpoints.ScheduleEventBody>
{
    private readonly IUserService _userService;
    private readonly IEventService _eventService;
    private readonly IGroupService _groupService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ScheduleEventBodyValidator(IClock clock, IUserService userService, IEventService eventService, IGroupService groupService, IHttpContextAccessor httpContextAccessor)
    {
        _userService = userService;
        _eventService = eventService;
        _groupService = groupService;
        _httpContextAccessor = httpContextAccessor;

        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("An event id must be provided.")
            .MustAsync(EnsureEventExistsAsync).WithMessage("Event does not exist.")
            .MustAsync(EnsureUserCanUpdateEventAsync).WithMessage("Lacking group permissions to update this event.")
            .MustAsync(EnsureEventCanBeScheduledAsync).WithMessage("Event cannot be scheduled with it's current status. Must be unlisted or announced.");

        RuleFor(x => x.StartTime)
            .NotEmpty().WithMessage("A start time must be provided.")
            .GreaterThan(_ => clock.GetCurrentInstant().Plus(Duration.FromMinutes(1))).WithMessage("Event start time cannot be in the past.");

        RuleFor(x => x.EndTime)
            .NotEmpty().WithMessage("An end time must be provided.")
            .GreaterThan(body => body.StartTime).WithMessage("Event end time must be after the start time.");
    }

    private Task<bool> EnsureEventExistsAsync(Guid id, CancellationToken _)
    {
        return _eventService.EventExistsAsync(id);
    }

    private async Task<bool> EnsureUserCanUpdateEventAsync(Guid id, CancellationToken _)
    {
        var groupId = await _eventService.GetEventGroupIdAsync(id);
        if (groupId == default)
            return false;

        return await ValidationMethods.EnsureUserCanUpdateGroupAsync(groupId, _httpContextAccessor, _userService, _groupService);
    }

    private Task<bool> EnsureEventCanBeScheduledAsync(Guid id, CancellationToken _)
    {
        return _eventService.CanScheduleEventAsync(id);
    }
}