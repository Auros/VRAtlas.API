using System.ComponentModel.DataAnnotations.Schema;

namespace VRAtlas.Models;

public class NotificationMetadata
{
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    public bool AtStart { get; set; }

    public bool AtThirtyMinutes { get; set; }
    
    public bool AtOneHour { get; set; }

    public bool AtOneDay { get; set; }
}