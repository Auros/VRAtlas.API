using FFMpegCore;
using FFMpegCore.Pipes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NodaTime;
using VRAtlas.Logging;
using VRAtlas.Models;
using VRAtlas.Options;

namespace VRAtlas.Services;

public interface IVideoService
{
    Task<bool> ExistsFromUploaderAsync(Guid id, Guid userId);
    Task<Guid> SaveAsync(string filePath, Guid? userId, long? length);
    Task<FileInfo> ScreenshotAsync(string filePath);
}

public class VideoService : IVideoService
{
    private readonly IClock _clock;
    private readonly IAtlasLogger _atlasLogger;
    private readonly AtlasContext _atlasContext;
    private readonly IOptions<VRAtlasOptions> _options;

    public VideoService(IClock clock, IAtlasLogger<VideoService> atlasLogger, AtlasContext atlasContext, IOptions<VRAtlasOptions> options)
    {
        _clock = clock;
        _options = options;
        _atlasLogger = atlasLogger;
        _atlasContext = atlasContext;
    }

    public Task<bool> ExistsFromUploaderAsync(Guid id, Guid userId) => _atlasContext.UploadRecords.AnyAsync(r => r.UserId == userId && r.Resource == id);

    public async Task<Guid> SaveAsync(string filePath, Guid? userId, long? length)
    {
        Guid id = Guid.NewGuid();
        var options = _options.Value;
        var now = _clock.GetCurrentInstant();

        _atlasLogger.LogInformation("{UserId} is generating videos with id {VideoResourceId}", userId, id);

        FileInfo fileInfo = new(filePath);
        
        string targetMp4Name = $"{id}.mp4";
        _atlasLogger.LogDebug("Generating audioless .mp4 version of {VideoResourceId}");
        
        // -an removes audio tracks
        await FFMpegArguments.FromFileInput(fileInfo)
            .OutputToFile(Path.Combine(options.CdnPath, targetMp4Name), true, options => options.WithCustomArgument("-an"))
            .ProcessAsynchronously();

        string targetWebmName = $"{id}.webm";
        string targetTemporaryWebmName = $"{id}.temp.webm";
        _atlasLogger.LogDebug("Generating audioless .webm version of {VideoResourceId} on separate thread");
        // We generate the webm on a separate thread because the codec conversion can take some time
        _ = Task.Run(async () =>
        {
            // We use a temporary file name so the cdn returns null until we finish processing the file
            await FFMpegArguments.FromFileInput(fileInfo)
                .OutputToFile(Path.Combine(options.CdnPath, targetTemporaryWebmName), true, options => options.WithCustomArgument("-an"))
                .ProcessAsynchronously();

            // Rename the video file now that its complete.
            File.Move(Path.Combine(options.CdnPath, targetTemporaryWebmName), Path.Combine(options.CdnPath, targetWebmName));
        });

        if (userId.HasValue && length.HasValue)
        {
            _atlasLogger.LogDebug("Creating record in database");

            UploadRecord record = new()
            {
                Resource = id,
                UploadedAt = now,
                Size = length.Value,
                UserId = userId.Value
            };

            _atlasContext.UploadRecords.Add(record);
            await _atlasContext.SaveChangesAsync();
        }

        return id;
    }

    public async Task<FileInfo> ScreenshotAsync(string filePath)
    {
        var options = _options.Value;

        var id = Guid.NewGuid();
        var savePath = Path.Combine(options.CdnPath, $"{id}.png");
        await FFMpeg.SnapshotAsync(filePath, savePath);
        return new FileInfo(savePath);
    }
}
