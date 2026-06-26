namespace Edge360.Domain.Enums;

/// <summary>
/// System-wide role used for platform administration and RBAC at the API boundary.
/// </summary>
public enum SystemRole
{
    /// <summary>Regular platform user.</summary>
    User = 0,

    /// <summary>Platform administrator: user/group oversight, audit access, retention policy.</summary>
    Administrator = 1
}
