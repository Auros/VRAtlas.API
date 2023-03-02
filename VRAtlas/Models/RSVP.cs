using System.ComponentModel.DataAnnotations.Schema;

namespace VRAtlas.Models;

public class RSVP
{
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Guid Id { get; set; }

    public int? Capacity { get; set; }
}