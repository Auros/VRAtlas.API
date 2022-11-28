using System.ComponentModel.DataAnnotations;

namespace VRAtlas.Options;

public class Auth0Options
{
    public const string Name = "Auth0";

    [Required]
    public required string Domain { get; set; }

    [Required]
    public required string Audience { get; set; }
}