namespace Edge360.Domain.Common;

/// <summary>
/// Base type for all persisted aggregate roots / entities.
/// Uses a GUID surrogate key and tracks creation/modification timestamps in UTC.
/// </summary>
public abstract class Entity
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? UpdatedAt { get; set; }

    public void Touch() => UpdatedAt = DateTimeOffset.UtcNow;
}
