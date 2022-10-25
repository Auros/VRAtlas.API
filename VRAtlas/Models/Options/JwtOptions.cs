namespace VRAtlas.Models.Options;

public class JwtOptions
{
    public string Key { get; set; } = null!;

    public string Issuer { get; set; } = null!;

    public string Audience { get; set; } = null!;

    public float TokenLifetimeInHours { get; set; }
}