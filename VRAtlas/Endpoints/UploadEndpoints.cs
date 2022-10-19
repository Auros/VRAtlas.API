using System.Security.Claims;
using VRAtlas.Models;
using VRAtlas.Services;

namespace VRAtlas.Endpoints;

public static class UploadEndpoints
{
    public record UploadUrlBody(string UploadUrl);

    public static IEndpointRouteBuilder MapUploadEndpoints(this IEndpointRouteBuilder builder)
    {
        builder.MapGet("/upload/url", GetUploadUrl)
            .Produces<UploadUrlBody>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden)
            .RequireAuthorization("UploadUrl");

        return builder;
    }

    private static async Task<IResult> GetUploadUrl(ClaimsPrincipal principal, IVariantCdnService variantCdnService)
    {
        var uploadUrl = await variantCdnService.GetUploadUrl(principal.FindFirstValue(AtlasConstants.IdentifierClaimType));
        if (uploadUrl is null)
            return Results.Problem(statusCode: 500);

        UploadUrlBody body = new(uploadUrl);
        return Results.Ok(body);
    }
}