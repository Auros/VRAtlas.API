using FluentValidation;
using VRAtlas.Services;

namespace VRAtlas.Endpoints.Validators;

public class NotificationBodyValidator : AbstractValidator<NotificationEndpoints.NotificationBody>
{
	private readonly INotificationService _notificationService;

	public NotificationBodyValidator(INotificationService notificationService)
	{
		_notificationService = notificationService;

		RuleFor(x => x.Id)
			.NotEmpty().WithMessage("A notification id must be provided.")
			.MustAsync(EnsureNotificationExistsAsync).WithMessage("Cannot find notification!");
	}

    private Task<bool> EnsureNotificationExistsAsync(Guid id, CancellationToken _)
    {
		return _notificationService.ExistsAsync(id);
    }
}