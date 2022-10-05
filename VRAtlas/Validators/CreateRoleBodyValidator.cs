using FluentValidation;
using Microsoft.EntityFrameworkCore;
using VRAtlas.Models.Bodies;

namespace VRAtlas.Validators;

public class CreateRoleBodyValidator : AbstractValidator<CreateRoleBody>
{
	private readonly AtlasContext _atlasContext;

	public CreateRoleBodyValidator(AtlasContext atlasContext)
	{
		_atlasContext = atlasContext;

		RuleFor(x => x.Name).NotEmpty().MaximumLength(24).MustAsync(EnsureUniqueName);

		RuleFor(x => x.Permissions).ForEach(p => p.NotEmpty().MinimumLength(1).MaximumLength(256));
	}

	private async Task<bool> EnsureUniqueName(string name, CancellationToken cancellationToken)
	{
		var exists = await _atlasContext.Roles.AsNoTracking().AnyAsync(r => r.Name.ToLower() == name.ToLower(), cancellationToken);
		return !exists;
	}
}