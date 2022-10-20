using FluentValidation;
using VRAtlas.Models.Bodies;

namespace VRAtlas.Validators;

public class UpdateGroupBodyValidator : AbstractValidator<UpdateGroupBody>
{
    public UpdateGroupBodyValidator()
    {
        RuleFor(x => x.Id).NotEmpty();

        RuleFor(x => x.Name).NotEmpty();
    }
}