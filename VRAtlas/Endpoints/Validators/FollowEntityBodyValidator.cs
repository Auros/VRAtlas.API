using FluentValidation;
using static VRAtlas.Endpoints.FollowEndpoints;

namespace VRAtlas.Endpoints.Validators;

public class FollowEntityBodyValidator : AbstractValidator<FollowEntityBody>
{
	public FollowEntityBodyValidator()
	{
		RuleFor(x => x.Id)
			.NotEmpty().WithMessage("Missing or invalid entity id.");

		RuleFor(x => x.Type)
			.NotEmpty().WithMessage("Missing or invalid entity type.");

		RuleFor(x => x.Metadata)
			.NotNull().WithMessage("Missing notification metadata.");
	}
}