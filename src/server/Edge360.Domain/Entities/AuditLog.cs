using Edge360.Domain.Common;

namespace Edge360.Domain.Entities;

/// <summary>
/// Append-only record of a security- or privacy-relevant action for compliance/audit.
/// </summary>
public class AuditLog : Entity
{
    /// <summary>Acting user, if any (null for system/anonymous actions).</summary>
    public Guid? ActorUserId { get; set; }

    /// <summary>Action verb, e.g. "auth.login", "group.join", "place.delete".</summary>
    public string Action { get; set; } = null!;

    /// <summary>Type of the affected entity, e.g. "Group", "Place".</summary>
    public string? TargetType { get; set; }

    public string? TargetId { get; set; }

    /// <summary>Optional structured detail (JSON) describing the change.</summary>
    public string? Metadata { get; set; }

    public string? IpAddress { get; set; }
}
