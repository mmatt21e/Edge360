namespace Edge360.Domain.Enums;

/// <summary>
/// Role of a user within a single family group. Distinct from system-wide roles.
/// </summary>
public enum MemberRole
{
    /// <summary>Group owner/administrator: full control over members and settings.</summary>
    Admin = 0,

    /// <summary>Guardian/parent: monitors members, manages places, receives alerts.</summary>
    Guardian = 1,

    /// <summary>Standard member (e.g. teen/child): shares location, limited privileges.</summary>
    Member = 2
}
