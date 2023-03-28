using System.ComponentModel.DataAnnotations;

namespace VRAtlas.Models.Crossposters;

public class VRCC : CrosspostSource
{
    [Required]
    public required Uri ApiUrl { get; set; }

    public int EventDurationInHours { get; set; } = 5;
}
