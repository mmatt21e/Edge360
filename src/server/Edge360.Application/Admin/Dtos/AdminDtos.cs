namespace Edge360.Application.Admin.Dtos;

public sealed record AdminUserDto(
    Guid Id,
    string Email,
    string DisplayName,
    string SystemRole,
    bool IsActive,
    int GroupCount,
    int DeviceCount,
    DateTimeOffset? LastLoginAt,
    DateTimeOffset CreatedAt);

public sealed record SetRoleRequest(string SystemRole);

public sealed record SetActiveRequest(bool IsActive);

public sealed record AdminGroupDto(
    Guid Id,
    string Name,
    string InviteCode,
    int MemberCount,
    int PlaceCount,
    Guid CreatedByUserId,
    DateTimeOffset CreatedAt);

public sealed record AuditLogDto(
    Guid Id,
    Guid? ActorUserId,
    string Action,
    string? TargetType,
    string? TargetId,
    string? IpAddress,
    DateTimeOffset CreatedAt);

public sealed record SystemStatsDto(
    int UserCount,
    int ActiveUserCount,
    int GroupCount,
    int DeviceCount,
    long LocationPointCount,
    long EventsLast24h,
    long TripCount);

public sealed record RetentionPolicyDto(
    int LocationRetentionDays,
    int EventRetentionDays,
    int AuditRetentionDays,
    bool Enabled);

public sealed record RetentionResultDto(
    int LocationPointsDeleted,
    int EventsDeleted,
    int AuditLogsDeleted);
