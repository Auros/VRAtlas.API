using Bogus;
using NodaTime;
using VRAtlas.Models;

namespace VRAtlas.Tests.Unit;

internal class AtlasFakes
{
    public static Faker<UserMetadata> UserMetadata { get; } = new Faker<UserMetadata>()
        .RuleFor(p => p.Id, f => f.Random.Guid())
        .RuleFor(p => p.CurrentSocialPlatformUsername, f => f.Person.UserName)
        .RuleFor(p => p.CurrentSocialPlatformProfilePicture, f => f.Internet.Avatar());

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

    public static Faker<GroupMember> GroupMember { get; } = new Faker<GroupMember>()
        .RuleFor(p => p.Id, f => f.Random.Guid())
        .RuleFor(p => p.User, _ => User.Generate())
        .RuleFor(p => p.Role, _ => GroupMemberRole.Owner)
        .RuleFor(p => p.JoinedAt, _ => SystemClock.Instance.GetCurrentInstant());

    public static Faker<Group> Group { get; } = new Faker<Group>()
        .RuleFor(p => p.Id, f => f.Random.Guid())
        .RuleFor(p => p.Name, f => f.Company.CompanyName())
        .RuleFor(p => p.Description, f => f.Company.CatchPhrase())
        .RuleFor(p => p.Icon, f => f.Random.Guid())
        .RuleFor(p => p.Banner, f => f.Random.Guid())
        .RuleFor(p => p.CreatedAt, f => SystemClock.Instance.GetCurrentInstant())
        .RuleFor(p => p.Members, f => f.Make(1, () => GroupMember.Generate()));
}