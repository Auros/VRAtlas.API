namespace VRAtlas.Models.Bodies;

public class UpdateGroupBody
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    public string? IconImageId { get; set; } = null;
    public string? BannerImageId { get; set; } = null;
}