using Edge360.Domain.Common;
using Edge360.Domain.Enums;

namespace Edge360.Domain.Entities;

/// <summary>
/// An event raised about a member within a group (arrival, departure, SOS, etc.).
/// Drives the activity feed and notification fan-out.
/// </summary>
public class SafetyEvent : Entity
{
    public Guid GroupId { get; set; }
    public Group Group { get; set; } = null!;

    /// <summary>The member the event concerns.</summary>
    public Guid SubjectUserId { get; set; }

    public EventType Type { get; set; }
    public EventSeverity Severity { get; set; } = EventSeverity.Info;

    public string Message { get; set; } = null!;

    /// <summary>Optional related place (for arrival/departure).</summary>
    public Guid? PlaceId { get; set; }
    public Place? Place { get; set; }

    public double? Latitude { get; set; }
    public double? Longitude { get; set; }

    public DateTimeOffset OccurredAt { get; set; } = DateTimeOffset.UtcNow;

    public bool Acknowledged { get; set; }

    public ICollection<Alert> Alerts { get; set; } = new List<Alert>();
}
