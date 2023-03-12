using Microsoft.EntityFrameworkCore;
using NodaTime;
using VRAtlas.Logging;
using VRAtlas.Models;

namespace VRAtlas.Services;

public interface ITagService
{
    /// <summary>
    /// Checks if a tag exists.
    /// </summary>
    /// <param name="name">The name of the tag.</param>
    /// <returns>Does the tag exist?</returns>
    Task<bool> TagExistsAsync(string name);

    /// <summary>
    /// Gets a tag if it exists.
    /// </summary>
    /// <param name="name">The name of the tag. Case insensitive.</param>
    /// <returns>The tag, or null if it does not exist.</returns>
    Task<Tag?> GetTagAsync(string name);

    /// <summary>
    /// Creates a tag asynchronously
    /// </summary>
    /// <param name="name">The name of the tag</param>
    /// <param name="creatorUserId"></param>
    /// <returns></returns>
    Task<Tag> CreateTagAsync(string name, Guid creatorUserId);
}

public class TagService : ITagService
{
    private readonly IClock _clock;
    private readonly IAtlasLogger _atlasLogger;
    private readonly AtlasContext _atlasContext;

    public TagService(IClock clock, IAtlasLogger<TagService> atlasLogger, AtlasContext atlasContext)
    {
        _clock = clock;
        _atlasLogger = atlasLogger;
        _atlasContext = atlasContext;
    }

    public async Task<Tag> CreateTagAsync(string name, Guid creatorUserId)
    {
        var tag = await GetTagAsync(name);
        if (tag is not null)
            return tag;

        _atlasLogger.LogInformation("User {UserId} has initiated the creation of the tag {TagName}", creatorUserId, name);
        var user = await _atlasContext.Users.FirstOrDefaultAsync(u => u.Id == creatorUserId);
        var now = _clock.GetCurrentInstant();
        
        tag = new Tag
        {
            Name = name,
            CreatedAt = now,
            CreatedBy = user,
            Id = Guid.NewGuid()
        };

        _atlasContext.Tags.Add(tag);
        await _atlasContext.SaveChangesAsync();
        _atlasLogger.LogInformation("Successfully created the tag {TagName}", name);
        return tag;
    }

    public Task<Tag?> GetTagAsync(string name)
    {
        return _atlasContext.Tags.FirstOrDefaultAsync(t => t.Name.ToLower() == name.ToLower());
    }

    public Task<bool> TagExistsAsync(string name)
    {
        return _atlasContext.Tags.AnyAsync(t => t.Name.ToLower() == name.ToLower());
    }
}