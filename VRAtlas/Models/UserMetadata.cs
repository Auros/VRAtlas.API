using System.ComponentModel.DataAnnotations.Schema;

namespace VRAtlas.Models;

public class UserMetadata
{
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Guid Id { get; set; }

    public required string CurrentSocialPlatformUsername { get; set; }

    public required string CurrentSocialPlatformProfilePicture { get; set; }

    public bool SynchronizeUsernameWithSocialPlatform { get; set; }

    public bool SynchronizeProfilePictureWithSocialPlatform { get; set; }

    [NotMapped]
    public Uri ProfilePictureUrl => new(CurrentSocialPlatformProfilePicture);

}
