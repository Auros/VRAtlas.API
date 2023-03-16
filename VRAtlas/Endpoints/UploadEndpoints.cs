using Microsoft.Extensions.Options;
using System.Security.Claims;
using VRAtlas.Attributes;
using VRAtlas.Endpoints.Internal;
using VRAtlas.Filters;
using VRAtlas.Options;
using VRAtlas.Services;

namespace VRAtlas.Endpoints;

public class UploadEndpoints : IEndpointCollection
{
    [VisualName("Upload URL")]
    public record UploadUrlBody(Uri UploadUrl);

    [VisualName("Video Upload")]
    public record UploadVideoBody(Guid Id);

    public static void BuildEndpoints(IEndpointRouteBuilder app)
    {
        app.MapGet("/upload/url", GetUploadUrl)
            .Produces<UploadUrlBody>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden)
            .RequireAuthorization("create:upload_url")
            .WithTags("Upload");

        app.MapPost("/upload/video", UploadVideo)
            .Produces<UploadVideoBody>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden)
            .RequireAuthorization("create:upload_video")
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

    private static async Task<IResult> UploadVideo(IFormFile file, ClaimsPrincipal principal, IUserService userService, IVideoService videoService, IOptions<VRAtlasOptions> options)
    {
        var user = await userService.GetUserAsync(principal);
        if (user is null)
            return Results.Unauthorized();

        var path = options.Value.CdnPath;
        var maxLength = options.Value.MaximumFileSizeLength;
        if (file.Length == 0)
            return Results.BadRequest(new FilterValidationResponse(new string[] { "Video file contains no data." }));

        if (file.Length > maxLength)
            return Results.BadRequest(new FilterValidationResponse(new string[] { "Video file size is too large." }));

        var ext = Path.GetExtension(file.FileName);
        if (!ext.Equals(".mp4", StringComparison.InvariantCultureIgnoreCase) && !ext.Equals(".webm", StringComparison.InvariantCultureIgnoreCase))
            return Results.BadRequest(new FilterValidationResponse(new string[] { "Invalid file type." }));

        var filePath = Path.Combine(path, $"{Guid.NewGuid()}.tmp{ext}");
        using var stream = File.Create(filePath);
        await file.CopyToAsync(stream);
        await stream.DisposeAsync();

        var resourceId = await videoService.SaveAsync(filePath, user.Id, file.Length);

        return Results.Ok(new UploadVideoBody(resourceId));
    }
}