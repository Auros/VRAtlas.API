using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace VRAtlas.Models;

public class RSVP
{
    [JsonIgnore]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Guid Id { get; set; }

    public int? Capacity { get; set; }
}