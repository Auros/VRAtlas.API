using NodaTime;
using System.ComponentModel.DataAnnotations.Schema;

namespace VRAtlas.Models;

public class UploadRecord
{
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    public long Size { get; set; }

    public Guid UserId { get; set; }

    public Guid Resource { get; set; }

    public Instant UploadedAt { get; set; }
}