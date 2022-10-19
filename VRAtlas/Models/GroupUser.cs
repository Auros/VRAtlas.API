namespace VRAtlas.Models;

public class GroupUser
{
    public User User { get; set; } = null!;
    public GroupRole Role { get; set; }
}