using Bogus;
using NodaTime;
using VRAtlas.Models;

namespace VRAtlas.Tests.Unit;

internal class AtlasFakes
{
    public static Faker<UserMetadata> UserMetadata { get; } = new Faker<UserMetadata>()
        .RuleFor(p => p.Id, f => f.Random.Guid())
        .RuleFor(p => p.CurrentSocialPlatformUsername, f => f.Person.UserName)
        .RuleFor(p => p.CurrentSocialPlatformProfilePicture, f => f.Image.PicsumUrl());

    public static Faker<User> User { get; } = new Faker<User>()
        .RuleFor(p => p.Id, f => f.Random.Guid())
        .RuleFor(p => p.Username, f => f.Person.UserName)
        .RuleFor(p => p.Picture, f => f.Random.Guid())
        .RuleFor(p => p.SocialId, f => f.Random.Word())
        .RuleFor(p => p.Metadata, (f, u) =>
        {
            var metadata = UserMetadata.Generate();
            metadata.CurrentSocialPlatformUsername = u.SocialId;
            return metadata;
        })
        .RuleFor(p => p.JoinedAt, f => SystemClock.Instance.GetCurrentInstant())
        .RuleFor(p => p.LastLoginAt, f => SystemClock.Instance.GetCurrentInstant());
}