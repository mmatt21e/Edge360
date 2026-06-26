using Edge360.Application.Common.Abstractions;
using Edge360.Domain.Entities;

namespace Edge360.Infrastructure.Auditing;

/// <summary>Persists audit entries to the append-only audit log table.</summary>
public sealed class AuditWriter : IAuditWriter
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;

    public AuditWriter(IAppDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task WriteAsync(
        string action,
        Guid? actorUserId = null,
        string? targetType = null,
        string? targetId = null,
        string? metadata = null,
        CancellationToken ct = default)
    {
        _db.AuditLogs.Add(new AuditLog
        {
            Action = action,
            ActorUserId = actorUserId ?? _currentUser.UserId,
            TargetType = targetType,
            TargetId = targetId,
            Metadata = metadata,
            IpAddress = _currentUser.IpAddress
        });

        await _db.SaveChangesAsync(ct);
    }
}
