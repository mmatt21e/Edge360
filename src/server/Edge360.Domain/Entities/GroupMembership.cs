using Edge360.Domain.Common;
using Edge360.Domain.Enums;

namespace Edge360.Domain.Entities;

/// <summary>
/// Join entity linking a <see cref="User"/> to a <see cref="Group"/> with a role and
/// per-group privacy preferences.
/// </summary>
public class GroupMembership : Entity
{
    public Guid GroupId { get; set; }
    public Group Group { get; set; } = null!;

    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public MemberRole Role { get; set; } = MemberRole.Member;

    /// <summary>When false, this member's location is hidden from the group (privacy toggle).</summary>
    public bool LocationSharingEnabled { get; set; } = true;

    /// <summary>Optional end time for a temporary sharing window; null means indefinite.</summary>
    public DateTimeOffset? SharingPausedUntil { get; set; }

    public bool IsSharingActive =>
        LocationSharingEnabled && (SharingPausedUntil is null || SharingPausedUntil <= DateTimeOffset.UtcNow);
}
