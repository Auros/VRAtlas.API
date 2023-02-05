using System.ComponentModel;
using System.Security.Claims;
using VRAtlas.Endpoints.Internal;
using VRAtlas.Services;

namespace VRAtlas.Endpoints;

public class UploadEndpoints : IEndpointCollection
{
    [DisplayName("Upload URL")]
    public record UploadUrlBody(Uri UploadUrl);

    public static void BuildEndpoints(IEndpointRouteBuilder app)
    {
        app.MapGet("/upload/url", GetUploadUrl)
            .Produces<UploadUrlBody>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden)
            .RequireAuthorization("read:upload_url")
            .WithTags("Upload");
    }

    private static async Task<IResult> GetUploadUrl(ClaimsPrincipal principal, IUserService userService, IImageCdnService imageCdnService)
    {
        var user = await userService.GetUserAsync(principal);
        if (user is null)
            return Results.Unauthorized();

        var uploadUrl = await imageCdnService.GetUploadUriAsync(user.Id);
        UploadUrlBody body = new(uploadUrl);
        return Results.Ok(body);
    }
}