using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;

namespace VRAtlas.Models;

[DisplayName("Notification Info")]
public class NotificationMetadata
{
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    public bool AtStart { get; set; }

    public bool AtThirtyMinutes { get; set; }
    
    public bool AtOneHour { get; set; }

    public bool AtOneDay { get; set; }
}