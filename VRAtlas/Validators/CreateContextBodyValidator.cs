using FluentValidation;
using VRAtlas.Models.Bodies;

namespace VRAtlas.Validators;

public class CreateContextBodyValidator : AbstractValidator<CreateContextBody>
{
    public CreateContextBodyValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(24);

        RuleFor(x => x.IconImageId).NotEmpty();
    }
}