namespace Edge360.Application.Driving;

/// <summary>
/// Tunable thresholds for driving analysis. Bound from configuration section "Driving";
/// defaults are reasonable starting points (note: speeding uses a single global limit, as
/// no per-road speed-limit data source is integrated yet).
/// </summary>
public sealed class DrivingThresholds
{
    public const string SectionName = "Driving";

    /// <summary>A time gap larger than this (minutes) between samples splits trips.</summary>
    public double TripGapMinutes { get; set; } = 5;

    /// <summary>Trips shorter than this (meters) are discarded as noise.</summary>
    public double MinTripDistanceMeters { get; set; } = 200;

    /// <summary>Speed above this (m/s) counts as "moving" for cornering evaluation.</summary>
    public double MinMovingSpeedMps { get; set; } = 1.5;

    /// <summary>Deceleration at or below this (m/s²; negative) is hard braking.</summary>
    public double HardBrakingMps2 { get; set; } = -3.5;

    /// <summary>Acceleration at or above this (m/s²) is harsh acceleration.</summary>
    public double HardAccelerationMps2 { get; set; } = 3.0;

    /// <summary>Heading change rate at or above this (degrees/second) is harsh cornering.</summary>
    public double HarshCorneringDegPerSec { get; set; } = 35;

    /// <summary>Speed above this (m/s) is speeding. Default ≈ 120 km/h.</summary>
    public double SpeedingLimitMps { get; set; } = 33.3;

    /// <summary>Minimum seconds between two events of the same type (debounce).</summary>
    public double EventCooldownSeconds { get; set; } = 3;

    // Score penalties per event.
    public int HardBrakingPenalty { get; set; } = 5;
    public int HardAccelerationPenalty { get; set; } = 3;
    public int HarshCorneringPenalty { get; set; } = 2;
    public int SpeedingPenalty { get; set; } = 4;
}
