using Microsoft.AspNetCore.Authorization;

namespace VRAtlas.Authorization;

public class AtlasPermissionRequirement : IAuthorizationRequirement
{
    public IReadOnlyList<string> Permissions { get; set; }

	public AtlasPermissionRequirement(params string[] permissions)
	{
		if (permissions.Length == 0)
			throw new Exception($"{nameof(AtlasPermissionRequirement)} must contain at least one requirement.");

		Permissions = permissions;
	}
}