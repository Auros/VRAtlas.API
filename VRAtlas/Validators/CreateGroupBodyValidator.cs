using FluentValidation;
using VRAtlas.Models.Bodies;

namespace VRAtlas.Validators;

public class CreateGroupBodyValidator : AbstractValidator<CreateGroupBody>
{
    public CreateGroupBodyValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MinimumLength(3).MaximumLength(24);

        RuleFor(x => x.IconImageId).NotEmpty();

        RuleFor(x => x.BannerImageId).NotEmpty();
    }
}