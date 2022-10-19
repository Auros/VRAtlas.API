using FluentValidation;
using VRAtlas.Models.Bodies;

namespace VRAtlas.Validators;

public class UpdateContextBodyValidator : AbstractValidator<UpdateContextBody>
{
    public UpdateContextBodyValidator()
    {
        RuleFor(x => x.Id).NotEmpty();

        RuleFor(x => x.Name).NotEmpty();

        RuleFor(x => x.Description).NotEmpty();
    }
}