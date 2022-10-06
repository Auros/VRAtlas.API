﻿using FluentValidation;
using Microsoft.EntityFrameworkCore;
using VRAtlas.Filters;
using VRAtlas.Models;
using VRAtlas.Models.Bodies;
using VRAtlas.Services;
using VRAtlas.Validators;

namespace VRAtlas.Endpoints;

public static class RoleEndpoints 
{
    public static IServiceCollection ConfigureRoleEndpoints(this IServiceCollection services)
    {
        services.AddScoped<IValidator<Role>, RoleValidator>();
        services.AddSingleton<IValidator<UpdateRoleBody>, UpdateRoleBodyValidator>();
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

        builder.MapPost("/roles/update", UpdateRole)
               .Produces<Role>(StatusCodes.Status200OK)
               .Produces(StatusCodes.Status400BadRequest)
               .Produces(StatusCodes.Status401Unauthorized)
               .AddEndpointFilter<ValidationFilter<UpdateRoleBody>>()
               .RequireAuthorization("EditRole");

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

    internal static async Task<IResult> UpdateRole(UpdateRoleBody body, ILogger<Role> logger, AtlasContext atlasContext, IUserPermissionService userPermissionService)
    {
        var role = await atlasContext.Roles.FirstOrDefaultAsync(r => r.Name.ToLower() == body.Name.ToLower());
        if (role is null)
            return Results.BadRequest(new Error { ErrorName = $"Role '{body.Name}' does not exist." });

        role.Permissions = body.Permissions;
        await atlasContext.SaveChangesAsync();

        // Since this role was updated, we want to clear any caches that store user permissions.
        await foreach (var userId in atlasContext.Users.Where(u => u.Roles.Contains(role)).Select(u => u.Id).AsAsyncEnumerable())
            await userPermissionService.Clear(userId); // Clears the permission cache for this specific user

        return Results.Ok(role);
    }
}