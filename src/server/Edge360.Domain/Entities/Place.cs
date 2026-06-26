using Edge360.Domain.Common;
using Edge360.Domain.Enums;
using Edge360.Domain.ValueObjects;

namespace Edge360.Domain.Entities;

/// <summary>
/// A named, radius-based geofence within a group (e.g. "Home", "School").
/// </summary>
public class Place : Entity
{
    public Guid GroupId { get; set; }
    public Group Group { get; set; } = null!;

    public string Name { get; set; } = null!;

    public double Latitude { get; set; }
    public double Longitude { get; set; }

    /// <summary>Geofence radius in meters.</summary>
    public double RadiusMeters { get; set; }

    /// <summary>When true, arrival/departure events are emitted for this place.</summary>
    public bool NotifyOnArrival { get; set; } = true;
    public bool NotifyOnDeparture { get; set; } = true;

    public GeoCoordinate Center => new(Latitude, Longitude);

    /// <summary>True when the given coordinate falls inside this place's radius.</summary>
    public bool Contains(GeoCoordinate point) => Center.IsWithin(point, RadiusMeters);

    /// <summary>
    /// Classifies movement relative to this place given whether the member was previously inside.
    /// </summary>
    public PlaceTransition Evaluate(bool wasInside, GeoCoordinate current)
    {
        var isInside = Contains(current);
        return (wasInside, isInside) switch
        {
            (false, true) => PlaceTransition.Entered,
            (true, false) => PlaceTransition.Exited,
            _ => PlaceTransition.None
        };
    }
}
