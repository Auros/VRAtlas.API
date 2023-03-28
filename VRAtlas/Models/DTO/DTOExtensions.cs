namespace VRAtlas.Models.DTO;

public static class DTOExtensions
{
    public static EventDTO Map(this Event atlasEvent)
    {
        return new EventDTO
        {
            Id = atlasEvent.Id,
            Name = atlasEvent.Name,
            Media = atlasEvent.Media,
            Video = atlasEvent.Video,
            Status = atlasEvent.Status,
            EndTime = atlasEvent.EndTime,
            AutoStart = atlasEvent.AutoStart,
            Crosspost = atlasEvent.Crosspost,
            StartTime = atlasEvent.StartTime,
            Description = atlasEvent.Description,
            Tags = atlasEvent.Tags.Map(),
            Stars = atlasEvent.Stars.Map(),
            Owner = atlasEvent.Owner?.Map()
        };
    }

    public static GroupDTO Map(this Group group)
    {
        return new GroupDTO
        {
            Id = group.Id,
            Name = group.Name,
            Icon = group.Icon,
            Banner = group.Banner,
            Identity = group.Identity,
            Description = group.Description,
            Members = group.Members.Map(),
        };
    }

    public static GroupMemberDTO Map(this GroupMember groupMember)
    {
        return new GroupMemberDTO
        {
            Role = groupMember.Role,
            User = groupMember.User!.Map(),
        };
    }

    public static UserDTO Map(this User user)
    {
        return new UserDTO
        {
            Id = user.Id,
            Links = user.Links,
            Picture = user.Picture,
            Username = user.Username,
            Biography = user.Biography,
            ProfileStatus = user.ProfileStatus
        };
    }

    public static EventStarDTO Map(this EventStar star)
    {
        return new EventStarDTO
        {
            Title = star.Title,
            Status = star.Status,
            User = star.User!.Map(),
        };
    }

    public static NotificationDTO Map(this Notification notification)
    {
        return new NotificationDTO
        {
            Id = notification.Id,
            Key = notification.Key,
            Read = notification.Read,
            Title = notification.Title,
            UserId = notification.UserId,
            EntityId = notification.EntityId,
            CreatedAt = notification.CreatedAt,
            EntityType = notification.EntityType,
            Description = notification.Description,
        };
    }

    public static NotificationInfoDTO Map(this NotificationMetadata meta)
    {
        return new NotificationInfoDTO
        {
            AtStart = meta.AtStart,
            AtOneDay = meta.AtOneDay,
            AtOneHour = meta.AtOneHour,
            AtThirtyMinutes = meta.AtThirtyMinutes,
        };
    }

    public static FollowDTO Map(this Follow follow)
    {
        return new FollowDTO
        {
            UserId = follow.UserId,
            EntityId = follow.EntityId,
            EntityType = follow.EntityType,
            FollowedAt = follow.FollowedAt,
            Metadata = follow.Metadata.Map()
        };
    }

    public static IEnumerable<GroupMemberDTO> Map(this IEnumerable<GroupMember>? groupMembers)
    {
        if (groupMembers is null || !groupMembers.Any())
            return Enumerable.Empty<GroupMemberDTO>();
        return groupMembers.Select(m => m.Map());
    }

    public static IEnumerable<EventStarDTO> Map(this IEnumerable<EventStar>? stars)
    {
        if (stars is null || !stars.Any())
            return Enumerable.Empty<EventStarDTO>();
        return stars.Select(s => s.Map());
    }

    public static IEnumerable<string> Map(this IEnumerable<EventTag>? tags)
    {
        if (tags is null || !tags.Any())
            return Enumerable.Empty<string>();
        return tags.Select(t => t.Tag.Name);
    }

    public static IEnumerable<EventDTO> Map(this IEnumerable<Event>? events)
    {
        if (events is null || !events.Any())
            return Enumerable.Empty<EventDTO>();
        return events.Select(e => e.Map());
    }

    public static IEnumerable<GroupDTO> Map(this IEnumerable<Group>? groups)
    {
        if (groups is null || !groups.Any())
            return Enumerable.Empty<GroupDTO>();
        return groups.Select(g => g.Map());
    }

    public static IEnumerable<NotificationDTO> Map(this IEnumerable<Notification>? notifs)
    {
        if (notifs is null || !notifs.Any())
            return Enumerable.Empty<NotificationDTO>();
        return notifs.Select(n => n.Map());
    }

    public static IEnumerable<UserDTO> Map(this IEnumerable<User>? users)
    {
        if (users is null || !users.Any())
            return Enumerable.Empty<UserDTO>();
        return users.Select(n => n.Map());
    }
}