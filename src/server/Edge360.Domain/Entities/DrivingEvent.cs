using Edge360.Domain.Common;
using Edge360.Domain.Enums;

namespace Edge360.Domain.Entities;

/// <summary>A single harsh-driving or speeding incident detected within a <see cref="Trip"/>.</summary>
public class DrivingEvent : Entity
{
    public Guid TripId { get; set; }
    public Trip Trip { get; set; } = null!;

    /// <summary>Denormalized owning user for efficient cross-trip queries.</summary>
    public Guid UserId { get; set; }

    public DrivingEventType Type { get; set; }

    public DateTimeOffset OccurredAt { get; set; }

    public double Latitude { get; set; }
    public double Longitude { get; set; }

    /// <summary>
    /// Severity magnitude in the unit natural to the type: m/s² for braking/acceleration,
    /// degrees/second for cornering, m/s for speeding.
    /// </summary>
    public double Magnitude { get; set; }
}
