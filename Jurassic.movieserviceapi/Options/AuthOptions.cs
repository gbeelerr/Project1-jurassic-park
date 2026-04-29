namespace Jurassic.movieserviceapi.Options;

public sealed class AuthOptions
{
    public const string SectionName = "Auth";

    /// <summary>Symmetric signing key for HS256 (must be sufficiently long; use 32+ random bytes in production).</summary>
    public string JwtSigningKey { get; set; } = "";

    public string JwtIssuer { get; set; } = "jurassic-movieservice";

    public string JwtAudience { get; set; } = "jurassic-clients";

    public int AccessTokenMinutes { get; set; } = 15;

    public int RefreshTokenDays { get; set; } = 30;
}
