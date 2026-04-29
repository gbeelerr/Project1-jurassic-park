namespace Jurassic.movieserviceapi.Options;

public sealed class AuthOptions
{
    public const string SectionName = "Auth";

    public string JwtSigningKey { get; set; } = "";

    public string JwtIssuer { get; set; } = "jurassic-movieservice";

    public string JwtAudience { get; set; } = "jurassic-clients";

    public int AccessTokenMinutes { get; set; } = 15;

    public int RefreshTokenDays { get; set; } = 30;
}
