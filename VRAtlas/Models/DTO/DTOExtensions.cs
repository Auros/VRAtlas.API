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
            Status = atlasEvent.Status,
            EndTime = atlasEvent.EndTime,
            AutoStart = atlasEvent.AutoStart,
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
            Picture = user.Picture,
            Username = user.Username,
            Biography = user.Biography,
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

}
