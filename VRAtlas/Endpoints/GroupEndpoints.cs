using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using VRAtlas.Models;
using VRAtlas.Models.Bodies;
using VRAtlas.Services;
using VRAtlas.Validators;

namespace VRAtlas.Endpoints;

public static class GroupEndpoints
{
    public record GroupPage(Group[] Groups, Page Page);

    public static IServiceCollection ConfigureGroupEndpoints(this IServiceCollection services)
    {
        services.AddSingleton<IValidator<CreateGroupBody>, CreateGroupBodyValidator>();
        services.AddSingleton<IValidator<UpdateGroupBody>, UpdateGroupBodyValidator>();
        return services;
    }

    public static IEndpointRouteBuilder MapGroupEndpoints(this IEndpointRouteBuilder builder)
    {
        builder.MapGet("/groups", GetPaginatedGroups)
               .Produces<IEnumerable<Group>>(StatusCodes.Status200OK);

        builder.MapGet("/groups/by-id/{groupId}", GetGroupById)
               .Produces<Group>(StatusCodes.Status200OK)
               .Produces(StatusCodes.Status404NotFound);

        builder.MapPost("/groups/create", CreateGroup)
               .Produces<Group>(StatusCodes.Status200OK)
               .Produces(StatusCodes.Status400BadRequest)
               .Produces(StatusCodes.Status401Unauthorized)
               .Produces(StatusCodes.Status403Forbidden)
               .RequireAuthorization("CreateGroup");

        builder.MapPost("/groups/update", UpdateGroup)
               .Produces<Group>(StatusCodes.Status200OK)
               .Produces(StatusCodes.Status400BadRequest)
               .Produces(StatusCodes.Status401Unauthorized)
               .Produces(StatusCodes.Status403Forbidden)
               .Produces(StatusCodes.Status404NotFound)
               .RequireAuthorization("EditGroup");

        return builder;
    }

    internal static async Task<IResult> GetGroupById(Guid groupId, AtlasContext atlasContext)
    {
        var group = await atlasContext.Groups.Include(g => g.Users).ThenInclude(gu => gu.User).ThenInclude(gu => gu.Roles).FirstOrDefaultAsync(g => g.Id == groupId);
        if (group is null)
            return Results.NotFound();

        return Results.Ok(group);
    }

    internal static async Task<IResult> GetPaginatedGroups(AtlasContext atlasContext, int page = 0, string search = "")
    {
        const int pageSize = 10;

        // If the page number is less than 0, reset it to zero.
        if (0 > page)
            page = 0;

        var groups = await atlasContext.Groups
            .Where(g => search == string.Empty || g.Name.Contains(search) || (g.Description != string.Empty && g.Description.Contains(search)))
            .Skip(pageSize * page)
            .Take(pageSize)
            .ToArrayAsync();

        var groupCount = await atlasContext.Groups.CountAsync();

        // Divide the number of groups stored by the size of each page, then get the ceiling to ensure any extra elements get their own page.
        var pageCount = (int)Math.Ceiling(groupCount * 1f / pageSize);

        // Ensure that we have at least one page, even if there's no elements.
        if (pageCount == 0)
            pageCount = 1;

        Page pageInfo = new(page, pageCount);
        GroupPage groupPage = new(groups, pageInfo);
        return Results.Ok(groupPage);
    }

    internal static async Task<IResult> CreateGroup([FromBody] CreateGroupBody body, ClaimsPrincipal principal, AtlasContext atlasContext, IAuthService authService, IVariantCdnService variantCdnService)
    {
        var uploaderId = principal.FindFirstValue(AtlasConstants.IdentifierClaimType);

        var nameInUse = await atlasContext.Groups.AnyAsync(g => g.Name.ToLower() == body.Name.ToLower());
        if (nameInUse)
            return Results.BadRequest(new Error { ErrorName = "Name is already in use." });

        var icon = await variantCdnService.ValidateAsync(body.IconImageId, uploaderId);
        if (icon is null)
            return Results.BadRequest(new Error { ErrorName = "Invalid Icon Id" });

        var banner = await variantCdnService.ValidateAsync(body.BannerImageId, uploaderId);
        if (banner is null)
            return Results.BadRequest(new Error { ErrorName = "Invalid Banner Id" });

        var user = await authService.GetUserAsync(principal);
        if (user is null)
            return Results.BadRequest(new Error { ErrorName = "Unable to validate user. This should not happen." });

        Group group = new()
        {
            Id = Guid.NewGuid(),
            Name = body.Name,
            Description = body.Description,
            Banner = banner,
            Icon = icon,
            Users = new List<GroupUser>
            {
                new GroupUser
                {
                    Id = Guid.NewGuid(),
                    Role = GroupRole.Owner,
                    User = user
                }
            }
        };

        atlasContext.Groups.Add(group);
        await atlasContext.SaveChangesAsync();
        return Results.Ok(group);
    }

    internal static async Task<IResult> UpdateGroup([FromBody] UpdateGroupBody body, ClaimsPrincipal principal, AtlasContext atlasContext, IVariantCdnService variantCdnService)
    {
        var group = await atlasContext.Groups
            .Include(g => g.Users)
            .ThenInclude(gu => gu.User)
            .ThenInclude(gu => gu.Roles)
            .FirstOrDefaultAsync(g => g.Id == body.Id);

        if (group is null)
            return Results.NotFound();

        // If the name has been changed, check to see if it's not in use.
        if (!body.Name.Equals(group.Name, StringComparison.OrdinalIgnoreCase))
        {
            var nameInUse = await atlasContext.Groups.AnyAsync(g => g.Name.ToLower() == body.Name.ToLower());
            if (nameInUse)
                return Results.BadRequest(new Error { ErrorName = "Name is already in use." });
        }

        body.Name = body.Name;
        body.Description = body.Description;

        if (body.IconImageId is not null || body.BannerImageId is not null)
        {
            var uploaderId = principal.FindFirstValue(AtlasConstants.IdentifierClaimType);

            if (body.IconImageId is not null)
            {
                var icon = await variantCdnService.ValidateAsync(body.IconImageId, uploaderId);
                if (icon is null)
                    return Results.BadRequest(new Error { ErrorName = "Invalid Icon Id" });
                group.Icon = icon;
            }
            
            if (body.BannerImageId is not null)
            {
                var banner = await variantCdnService.ValidateAsync(body.BannerImageId, uploaderId);
                if (banner is null)
                    return Results.BadRequest(new Error { ErrorName = "Invalid Banner Id" });
                group.Banner = banner;
            }
        }

        await atlasContext.SaveChangesAsync();
        return Results.Ok(group);
    }
}