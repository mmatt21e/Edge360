using Edge360.Domain.Common;

namespace Edge360.Domain.Entities;

/// <summary>
/// A delivery record for notifying a specific recipient about a <see cref="SafetyEvent"/>.
/// The actual transport (push/email) is abstracted; this row tracks intent and outcome.
/// </summary>
public class Alert : Entity
{
    public Guid EventId { get; set; }
    public SafetyEvent Event { get; set; } = null!;

    public Guid RecipientUserId { get; set; }

    /// <summary>Transport channel, e.g. "push" or "email".</summary>
    public string Channel { get; set; } = null!;

    public bool Delivered { get; set; }
    public DateTimeOffset? DeliveredAt { get; set; }
    public string? FailureReason { get; set; }
}
