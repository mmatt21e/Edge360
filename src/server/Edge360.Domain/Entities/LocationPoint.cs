using Edge360.Domain.Common;
using Edge360.Domain.ValueObjects;

namespace Edge360.Domain.Entities;

/// <summary>
/// A single location sample. This is the high-volume, time-series table — indexed by
/// (UserId, RecordedAt) and intended for time-based partitioning/retention.
/// </summary>
public class LocationPoint : Entity
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public Guid? DeviceId { get; set; }
    public Device? Device { get; set; }

    public double Latitude { get; set; }
    public double Longitude { get; set; }

    /// <summary>Horizontal accuracy in meters, if reported by the device.</summary>
    public double? AccuracyMeters { get; set; }

    /// <summary>Speed in meters/second, if reported.</summary>
    public double? SpeedMps { get; set; }

    /// <summary>Heading in degrees [0,360), if reported.</summary>
    public double? Heading { get; set; }

    public int? BatteryLevel { get; set; }

    /// <summary>Timestamp the sample was captured on-device (UTC), may differ from <see cref="Entity.CreatedAt"/>.</summary>
    public DateTimeOffset RecordedAt { get; set; }

    public GeoCoordinate ToCoordinate() => new(Latitude, Longitude);
}
