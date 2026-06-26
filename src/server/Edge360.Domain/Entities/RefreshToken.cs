using Edge360.Domain.Common;

namespace Edge360.Domain.Entities;

/// <summary>
/// A single refresh token in a rotating chain. Tokens are stored hashed; rotation
/// marks the old token revoked and links it to its replacement for theft detection.
/// </summary>
public class RefreshToken : Entity
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    /// <summary>SHA-256 hash of the opaque token value handed to the client.</summary>
    public string TokenHash { get; set; } = null!;

    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? RevokedAt { get; set; }

    /// <summary>Hash of the token that replaced this one during rotation, if any.</summary>
    public string? ReplacedByTokenHash { get; set; }

    public bool IsActive => RevokedAt is null && DateTimeOffset.UtcNow < ExpiresAt;

    public void Revoke(string? replacedByTokenHash = null)
    {
        RevokedAt = DateTimeOffset.UtcNow;
        ReplacedByTokenHash = replacedByTokenHash;
    }
}
