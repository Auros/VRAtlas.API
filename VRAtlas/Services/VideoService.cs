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
    Task<Guid> SaveAsync(Stream stream, Guid userId);
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

    public async Task<Guid> SaveAsync(Stream stream, Guid userId)
    {
        Guid id = Guid.NewGuid();
        var options = _options.Value;
        var now = _clock.GetCurrentInstant();

        _atlasLogger.LogInformation("{UserId} is generating videos with id {VideoResourceId}", userId, id);

        _atlasLogger.LogDebug("Copying input stream to memory for {VideoResourceId}", id);

        using MemoryStream ms = new();
        await stream.CopyToAsync(ms);
        var length = ms.Length;

        StreamPipeSource source = new(ms);

        // -an removes audio tracks

        string targetMp4Name = $"{id}.mp4";
        _atlasLogger.LogDebug("Generating audioless .mp4 version of {VideoResourceId}");
        
        await FFMpegArguments.FromPipeInput(source)
            .OutputToFile(Path.Combine(options.CdnPath, targetMp4Name), true, options => options.WithCustomArgument("-an"))
            .ProcessAsynchronously();

        // Reset the memory stream so we can read from it again
        ms.Position = 0;

        string targetWebmName = $"{id}.webm";
        _atlasLogger.LogDebug("Generating audioless .webm version of {VideoResourceId}");

        await FFMpegArguments.FromPipeInput(source)
            .OutputToFile(Path.Combine(options.CdnPath, targetWebmName), true, options => options.WithCustomArgument("-an"))
            .ProcessAsynchronously();

        _atlasLogger.LogDebug("Creating record in database");

        UploadRecord record = new()
        {
            Size = length,
            Resource = id,
            UploadedAt = now,
            UserId = userId
        };

        _atlasContext.UploadRecords.Add(record);
        await _atlasContext.SaveChangesAsync();

        return record.Resource;
    }
}
