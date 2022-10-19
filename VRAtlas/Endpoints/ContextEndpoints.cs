using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using VRAtlas.Filters;
using VRAtlas.Models;
using VRAtlas.Models.Bodies;
using VRAtlas.Services;
using VRAtlas.Validators;

namespace VRAtlas.Endpoints;

public static class ContextEndpoints
{
    public static IServiceCollection ConfigureContextEndpoints(this IServiceCollection services)
    {
        services.AddSingleton<IValidator<CreateContextBody>, CreateContextBodyValidator>();
        services.AddSingleton<IValidator<UpdateContextBody>, UpdateContextBodyValidator>();
        return services;
    }

    public static IEndpointRouteBuilder MapContextEndpoints(this IEndpointRouteBuilder builder)
    {
        builder.MapGet("/contexts", GetAllContexts).Produces<IEnumerable<Context>>(StatusCodes.Status200OK);
        builder.MapPost("/contexts/create", CreateContext)
               .Produces<Context>(StatusCodes.Status200OK)
               .Produces(StatusCodes.Status400BadRequest)
               .Produces(StatusCodes.Status400BadRequest)
               .Produces(StatusCodes.Status401Unauthorized)
               .Produces(StatusCodes.Status403Forbidden)
               .AddEndpointFilter<ValidationFilter<CreateContextBody>>()
               .RequireAuthorization("ManageContexts");

        builder.MapPost("/contexts/update", UpdateContext)
               .Produces<Context>(StatusCodes.Status200OK)
               .Produces(StatusCodes.Status400BadRequest)
               .Produces(StatusCodes.Status400BadRequest)
               .Produces(StatusCodes.Status401Unauthorized)
               .Produces(StatusCodes.Status403Forbidden)
               .AddEndpointFilter<ValidationFilter<UpdateContextBody>>()
               .RequireAuthorization("ManageContexts");

        return builder;
    }

    internal static async Task<IResult> GetAllContexts(AtlasContext atlasContext)
    {
        var context = await atlasContext.Contexts.ToArrayAsync();
        return Results.Ok(context);
    }

    internal static async Task<IResult> CreateContext([FromBody] CreateContextBody body, ClaimsPrincipal principal, AtlasContext atlasContext, IVariantCdnService variantCdnService)
    {
        var uploaderId = principal.FindFirstValue(AtlasConstants.IdentifierClaimType);

        var variants = await variantCdnService.ValidateAsync(body.IconImageId, uploaderId);
        if (variants is null)
            return Results.BadRequest(new Error { ErrorName = "Invalid Icon Id" });
        
        Context context = new()
        {
            Icon = variants,
            Name = body.Name,
            Type = body.Type,
            Id = Guid.NewGuid(),
            Description = body.Description,
        };

        atlasContext.Contexts.Add(context);
        await atlasContext.SaveChangesAsync();

        return Results.Ok(context);
    }

    internal static async Task<IResult> UpdateContext([FromBody] UpdateContextBody body, AtlasContext atlasContext)
    {
        var context = await atlasContext.Contexts.FirstOrDefaultAsync(c => c.Id == body.Id);
        if (context is null)
            return Results.NotFound();

        context.Name = body.Name;
        context.Type = body.Type;
        context.Description = body.Description;
    
        await atlasContext.SaveChangesAsync();

        return Results.Ok(context);
    }
}