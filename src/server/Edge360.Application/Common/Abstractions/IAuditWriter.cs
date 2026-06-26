namespace Edge360.Application.Common.Abstractions;

/// <summary>Records security/privacy-relevant actions to the append-only audit log.</summary>
public interface IAuditWriter
{
    Task WriteAsync(
        string action,
        Guid? actorUserId = null,
        string? targetType = null,
        string? targetId = null,
        string? metadata = null,
        CancellationToken ct = default);
}
