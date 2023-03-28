using Microsoft.AspNetCore.OutputCaching;
using Microsoft.Extensions.Options;
using NodaTime;
using System.Text.Json.Serialization;
using VRAtlas.Core.Models;
using VRAtlas.Events;
using VRAtlas.Logging;
using VRAtlas.Models;
using VRAtlas.Models.Crossposters;
using VRAtlas.Options;
using VRAtlas.Services;

namespace VRAtlas.Listeners;

public class VRCCSynchronizationListener : IScopedEventListener<CrosspostSynchronizationEvent>
{
    private readonly IAtlasLogger _atlasLogger;
    private readonly IEventService _eventService;
    private readonly IVideoService _videoService;
    private readonly IImageCdnService _imageCdnService;
    private readonly IOutputCacheStore _outputCacheStore;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IOptions<VRAtlasOptions> _vrAtlasOptions;
    private readonly ICrosspostingService _crosspostingService;
    private readonly IOptions<CrosspostingOptions> _crosspostingOptions;

    public VRCCSynchronizationListener(
        IEventService eventService,
        IVideoService videoService,
        IImageCdnService imageCdnService,
        IOutputCacheStore outputCacheStore,
        IHttpClientFactory httpClientFactory,
        IOptions<VRAtlasOptions> vrAtlasOptions,
        ICrosspostingService crosspostingService,
        IOptions<CrosspostingOptions> crosspostingOptions,
        IAtlasLogger<VRCCSynchronizationListener> atlasLogger)
    {
        _atlasLogger = atlasLogger;
        _eventService = eventService;
        _videoService = videoService;
        _vrAtlasOptions = vrAtlasOptions;
        _imageCdnService = imageCdnService;
        _outputCacheStore = outputCacheStore;
        _httpClientFactory = httpClientFactory;
        _crosspostingService = crosspostingService;
        _crosspostingOptions = crosspostingOptions;
    }

    public async Task Handle(CrosspostSynchronizationEvent _)
    {
        var vrcc = _crosspostingOptions.Value.VRCC;
        if (vrcc is null)
            throw new InvalidOperationException("VRCC options have not been configured. This should not happen.");

        var client = _httpClientFactory.CreateClient(nameof(VRCC));
        var vrAtlasClient = _httpClientFactory.CreateClient(nameof(VRAtlas));
        var group = await _crosspostingService.GetCrossposterGroupAsync(vrcc);

        var structuredEvents = await client.GetFromJsonAsync<StructuredEvents>("events_structured");
        structuredEvents ??= new StructuredEvents { Current = Array.Empty<VRCCEvent>(), Future = Array.Empty<VRCCEvent>(), Past = Array.Empty<VRCCEvent>() };
        var vrccEvents = structuredEvents.Future.Concat(structuredEvents.Current);

        int count = 0;
        var cdnPath = _vrAtlasOptions.Value.CdnPath;
        foreach (var vrccEvent in vrccEvents)
        {
            try
            {
                Guid? videoResourceId = null;
                var crosspostIdentifier = $"{vrcc.Source}event/{vrccEvent.Id}"; // The url on VRCC
                var atlasEvent = await _eventService.GetEventByCrosspostAsync(crosspostIdentifier);
                if (atlasEvent is null)
                {
                    _atlasLogger.LogInformation("Synchronizing new VRCC event {EventName} ({EventId})", vrccEvent.Name, vrccEvent.Id);

                    Guid resourceId;
                    Stream targetImageStream;
                    string targetFileName;
                    FileInfo? targetImageFile = null;

                    // Generate poster
                    var flyerUrlPath = vrccEvent.FlyerLink.AbsolutePath;
                    if (flyerUrlPath.ToLower().EndsWith(".mp4") || flyerUrlPath.ToLower().EndsWith(".webm"))
                    {
                        _atlasLogger.LogInformation("Video flyer detected, handling poster screenshot process");
                        var fileName = Path.GetFileNameWithoutExtension(vrccEvent.FlyerLink.AbsolutePath);

                        // Use the first frame from the video file as the poster media.
                        // Step 1: Save the video to the file stream
                        var ext = Path.GetExtension(flyerUrlPath);
                        using var stream = await vrAtlasClient.GetStreamAsync(vrccEvent.FlyerLink);
                        var filePath = Path.Combine(cdnPath, $"{Guid.NewGuid()}.tmp{ext}");
                        using var fileStream = File.Create(filePath);
                        await stream.CopyToAsync(fileStream);
                        await fileStream.DisposeAsync();

                        // Step 2: Take a screenshot of the video and then save it.
                        _atlasLogger.LogInformation("Generated local temporary file, generating screenshot");
                        targetImageFile = await _videoService.ScreenshotAsync(filePath);
                        videoResourceId = await _videoService.SaveAsync(filePath, null, null);

                        // Step 3: Delete the temporary video
                        _atlasLogger.LogInformation("Deleting local temporary file");
                        File.Delete(filePath);

                        targetFileName = targetImageFile.Name;
                        targetImageStream = targetImageFile.OpenRead();
                    }
                    else
                    {
                        // Prepare static poster
                        var fileName = Path.GetFileNameWithoutExtension(vrccEvent.FlyerLink.AbsolutePath);
                        targetImageStream = await vrAtlasClient.GetStreamAsync(vrccEvent.FlyerLink);
                        targetFileName = fileName;
                    }

                    // Upload the flyer to our image cdn.
                    _atlasLogger.LogInformation("Uploading static image poster");
                    try
                    {
                        resourceId = await _imageCdnService.UploadAsync(targetImageStream, targetFileName);
                    }
                    finally
                    {
                        await targetImageStream.DisposeAsync();
                        targetImageFile?.Delete();
                    }

                    // Create the event
                    _atlasLogger.LogInformation("Generating the event...");
                    atlasEvent = await _eventService.CreateEventAsync(vrccEvent.Name, group.Id, resourceId);
                }
                if (atlasEvent.Status == EventStatus.Announced || atlasEvent.Status == EventStatus.Unlisted)
                {
                    // Update non-media data
                    var startTime = Instant.FromUnixTimeSeconds((long)vrccEvent.StartTimestamp);
                    var endTime = startTime.Plus(Duration.FromHours(vrcc.EventDurationInHours));

                    var hasVideo = videoResourceId.HasValue || atlasEvent.Video.HasValue;

                    // Update the event info
                    // We pass in an empty guid for the updater property but its not used based on our other inputs (only used if tags.Length > 0 || stars.Length > 0)
                    await _eventService.UpdateEventAsync(atlasEvent.Id, vrccEvent.Name, vrccEvent.Description, null, Enumerable.Empty<string>(), Enumerable.Empty<EventStarInfo>(), Guid.Empty, true, hasVideo, videoResourceId, crosspostIdentifier);

                    // Schedule the event info if necessary
                    if (atlasEvent.StartTime != startTime)
                        await _eventService.ScheduleEventAsync(atlasEvent.Id, startTime, endTime);

                    // Announce the event if it hasn't been yet.
                    if (atlasEvent.Status == EventStatus.Unlisted)
                        await _eventService.AnnounceEventAsync(atlasEvent.Id);

                    count++;
                }
            }
            catch (Exception e)
            {
                _atlasLogger.LogCritical("Could not synchronize VRCC event {EventName} ({EventId}), {Exception}", vrccEvent.Name, vrccEvent.Name, e);
            }
        }

        if (count == 0)
            return;

        // Clear event cache if we've made changes
        await _outputCacheStore.EvictByTagAsync("events", default);
    }

    private class VRCCEvent
    {
        [JsonPropertyName("event_id")]
        public required Guid Id { get; set; }

        [JsonPropertyName("event_name")]
        public required string Name { get; set; }

        [JsonPropertyName("description")]
        public required string Description { get; set; }

        [JsonPropertyName("start_time_instant")]
        public required double StartTimestamp { get; set; } // the timestamp in the api response has a decimal point... :Aware:

        [JsonPropertyName("flyer_link")]
        public required Uri FlyerLink { get; set; }
    }

    private class StructuredEvents
    {
        [JsonPropertyName("current_events")]
        public required VRCCEvent[] Current { get; set; }

        [JsonPropertyName("future_events")]
        public required VRCCEvent[] Future { get; set; }

        [JsonPropertyName("past_events")]
        public required VRCCEvent[] Past { get; set; }
    }
}
