using FluentValidation;
using NodaTime;
using VRAtlas.Models.Bodies;

namespace VRAtlas.Validators;

public class ManageEventBodyValidator : AbstractValidator<ManageEventBody>
{
    public ManageEventBodyValidator(IClock clock)
    {
        RuleFor(x => x.Name).NotEmpty();

        RuleFor(x => x.StartTime).GreaterThan(clock.GetCurrentInstant());
        
        RuleFor(x => x.EndTime).GreaterThan(clock.GetCurrentInstant());
    }
}