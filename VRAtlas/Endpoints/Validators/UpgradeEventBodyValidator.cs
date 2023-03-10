using FluentValidation;
using Microsoft.EntityFrameworkCore;
using VRAtlas.Models;
using VRAtlas.Services;

namespace VRAtlas.Endpoints.Validators;

public class UpgradeEventBodyValidator : AbstractValidator<EventEndpoints.UpgradeEventBody>
{
    private readonly IUserService _userService;
    private readonly AtlasContext _atlasContext;
    private readonly IEventService _eventService;
    private readonly IGroupService _groupService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public UpgradeEventBodyValidator(IUserService userService, AtlasContext atlasContext, IEventService eventService, IGroupService groupService, IHttpContextAccessor httpContextAccessor)
    {
        _userService = userService;
        _atlasContext = atlasContext;
        _eventService = eventService;
        _groupService = groupService;
        _httpContextAccessor = httpContextAccessor;

        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("An event id must be provided.")
            .MustAsync(EnsureEventExistsAsync).WithMessage("Event does not exist.")
            .MustAsync(EnsureUserCanUpdateEventAsync).WithMessage("Lacking group permissions to update this event.")
            .MustAsync(EnsureUserCanUpgradeEventAsync).WithMessage("Group cannot have more than 3 active events.");
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

    private async Task<bool> EnsureUserCanUpgradeEventAsync(Guid id, CancellationToken token)
    {
        var groupId = await _eventService.GetEventGroupIdAsync(id);
        if (groupId == default)
            return false;

        var count = await _atlasContext.Events.CountAsync(e => e.Owner!.Id == id && (e.Status == EventStatus.Announced || e.Status == EventStatus.Started), token);
        return count < 3;
    }
}