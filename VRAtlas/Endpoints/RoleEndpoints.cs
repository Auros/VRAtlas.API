using Microsoft.EntityFrameworkCore;
using VRAtlas.Models;

namespace VRAtlas.Endpoints;

public static class RoleEndpoints 
{
    public static IEndpointRouteBuilder MapRoleEndpoints(this IEndpointRouteBuilder builder)
    {
        builder.MapGet("/roles", GetAllRoles).Produces<IEnumerable<Role>>(StatusCodes.Status200OK);
        return builder;
    }

    internal static async Task<IResult> GetAllRoles(AtlasContext atlasContext)
    {
        var roles = await atlasContext.Roles.AsNoTracking().ToArrayAsync();
        return Results.Ok(roles);
    }
}