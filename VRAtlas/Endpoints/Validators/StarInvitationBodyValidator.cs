using FluentValidation;
using VRAtlas.Models;
using VRAtlas.Services;

namespace VRAtlas.Endpoints.Validators;

public class StarInvitationBodyValidator : AbstractValidator<EventEndpoints.StarInvitationBody>
{
    private readonly IUserService _userService;
    private readonly IEventService _eventService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public StarInvitationBodyValidator(IUserService userService, IEventService eventService, IHttpContextAccessor httpContextAccessor)
    {
        _userService = userService;
        _eventService = eventService;
        _httpContextAccessor = httpContextAccessor;

        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("An event id must be provided.")
            .MustAsync(EnsureEventExistsAsync).WithMessage("Event does not exist.")
            .MustAsync(EnsureEventIsWritableAsync).WithMessage("Event has been concluded or canceled, cannot accept or reject invite.")
            .MustAsync(EnsureUserHasPendingInviteAsync).WithMessage("Missing invitation to event stars.");
    }

    private Task<bool> EnsureEventExistsAsync(Guid id, CancellationToken _)
    {
        return _eventService.EventExistsAsync(id);
    }

    private async Task<bool> EnsureEventIsWritableAsync(Guid id, CancellationToken _)
    {
        return (await _eventService.GetEventStatusAsync(id)) is EventStatus.Unlisted or EventStatus.Announced or EventStatus.Started;
    }

    private async Task<bool> EnsureUserHasPendingInviteAsync(Guid id, CancellationToken _)
    {
        var atlasEvent = await _eventService.GetEventByIdAsync(id);
        if (atlasEvent is null)
            return false;

        // Get the currently authenticated user.
        var principal = _httpContextAccessor.HttpContext?.User;
        if (principal is null)
            return false;

        // Ensure that they're a user within our database.
        var user = await _userService.GetUserAsync(principal);
        if (user is null)
            return false;

        return atlasEvent.Stars.Any(s => s.User!.Id == user.Id && s.Status is EventStarStatus.Pending);
    }
}