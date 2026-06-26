namespace Edge360.Infrastructure.Identity;

/// <summary>Bound from configuration section "Jwt".</summary>
public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "edge360";
    public string Audience { get; set; } = "edge360-clients";

    /// <summary>Symmetric signing key. MUST be overridden in production via configuration/secret.</summary>
    public string SigningKey { get; set; } = string.Empty;

    public int AccessTokenMinutes { get; set; } = 15;
}
