using Edge360.Domain.Common;

namespace Edge360.Domain.Entities;

/// <summary>
/// A reconstructed driving trip for a user: a contiguous run of movement with derived
/// distance/speed metrics, harsh-driving event counts, and a 0–100 safety score.
/// </summary>
public class Trip : Entity
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public DateTimeOffset StartedAt { get; set; }
    public DateTimeOffset EndedAt { get; set; }

    public double DistanceMeters { get; set; }
    public double DurationSeconds { get; set; }
    public double MaxSpeedMps { get; set; }
    public double AverageSpeedMps { get; set; }

    public int HardBrakingCount { get; set; }
    public int HardAccelerationCount { get; set; }
    public int HarshCorneringCount { get; set; }
    public int SpeedingCount { get; set; }

    /// <summary>Driving score in [0, 100]; 100 is flawless.</summary>
    public int Score { get; set; } = 100;

    public ICollection<DrivingEvent> Events { get; set; } = new List<DrivingEvent>();
}
