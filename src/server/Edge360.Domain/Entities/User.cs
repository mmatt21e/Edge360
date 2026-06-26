using Edge360.Domain.Common;
using Edge360.Domain.Enums;

namespace Edge360.Domain.Entities;

/// <summary>
/// A platform account. Credentials are never stored in plaintext — only a salted password hash.
/// </summary>
public class User : Entity
{
    public string Email { get; set; } = null!;

    /// <summary>Normalized (upper-invariant) email used for unique lookups.</summary>
    public string NormalizedEmail { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public string DisplayName { get; set; } = null!;

    public SystemRole SystemRole { get; set; } = SystemRole.User;

    public bool IsActive { get; set; } = true;

    // Multi-factor (TOTP) — optional, off by default.
    public bool MfaEnabled { get; set; }
    public string? MfaSecret { get; set; }

    public DateTimeOffset? LastLoginAt { get; set; }

    // Navigation
    public ICollection<Device> Devices { get; set; } = new List<Device>();
    public ICollection<GroupMembership> Memberships { get; set; } = new List<GroupMembership>();
    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
}
