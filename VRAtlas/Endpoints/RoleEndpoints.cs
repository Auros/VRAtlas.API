using FluentValidation;
using Microsoft.EntityFrameworkCore;
using VRAtlas.Filters;
using VRAtlas.Models;
using VRAtlas.Validators;

namespace VRAtlas.Endpoints;

public static class RoleEndpoints 
{
    public static IServiceCollection ConfigureRoleEndpoints(this IServiceCollection services)
    {
        services.AddScoped<IValidator<Role>, RoleValidator>();
        return services;
    }

    public static IEndpointRouteBuilder MapRoleEndpoints(this IEndpointRouteBuilder builder)
    {
        builder.MapGet("/roles", GetAllRoles)
               .Produces<IEnumerable<Role>>(StatusCodes.Status200OK);

        builder.MapPost("/roles/create", CreateRole)
               .Produces<Role>(StatusCodes.Status200OK)
               .Produces(StatusCodes.Status400BadRequest)
               .Produces(StatusCodes.Status401Unauthorized)
               .AddEndpointFilter<ValidationFilter<Role>>()
               .RequireAuthorization("CreateRole");

        return builder;
    }

    internal static async Task<IResult> GetAllRoles(AtlasContext atlasContext)
    {
        var roles = await atlasContext.Roles.AsNoTracking().ToArrayAsync();
        return Results.Ok(roles);
    }

    internal static async Task<IResult> CreateRole(Role role, ILogger<Role> logger, AtlasContext atlasContext)
    {
        logger.LogInformation("Creating a new role with the name {RoleName} with {RolePermissionCount} default permission(s)", role.Name, role.Permissions.Count);

        atlasContext.Roles.Add(role);
        await atlasContext.SaveChangesAsync();
        return Results.Ok(role);
    }
}