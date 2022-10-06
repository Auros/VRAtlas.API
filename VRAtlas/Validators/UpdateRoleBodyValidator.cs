using FluentValidation;
using VRAtlas.Models.Bodies;

namespace VRAtlas.Validators;

public class UpdateRoleBodyValidator : AbstractValidator<UpdateRoleBody>
{
    public UpdateRoleBodyValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(24);

        RuleFor(x => x.Permissions).ForEach(p => p.NotEmpty().MinimumLength(1).MaximumLength(256));
    }
}