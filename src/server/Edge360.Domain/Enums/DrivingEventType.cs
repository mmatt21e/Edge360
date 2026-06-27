namespace Edge360.Domain.Enums;

/// <summary>Per-trip driving telemetry events derived from the location stream.</summary>
public enum DrivingEventType
{
    HardBraking = 0,
    HardAcceleration = 1,
    HarshCornering = 2,
    Speeding = 3
}
