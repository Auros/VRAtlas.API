using FluentValidation;

namespace VRAtlas.Endpoints.Validators;

public class UpdateUserBodyValidator : AbstractValidator<UserEndpoints.UpdateUserBody>
{
	public UpdateUserBodyValidator()
	{
		RuleFor(x => x.Links)
			.NotNull().WithMessage("Links must be provided (even if empty).");

		RuleFor(x => x.Biography)
			.NotNull().WithMessage("Biography must be provided (even if empty).");

		RuleFor(x => x.Notifications)
			.NotNull().WithMessage("Notification settings must be provideed (even if empty).");
	}
}