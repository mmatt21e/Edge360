namespace Edge360.Domain.ValueObjects;

/// <summary>
/// Immutable latitude/longitude pair with great-circle distance helpers.
/// Latitude is constrained to [-90, 90] and longitude to [-180, 180].
/// </summary>
public readonly record struct GeoCoordinate
{
    private const double EarthRadiusMeters = 6_371_000d;

    public double Latitude { get; }
    public double Longitude { get; }

    public GeoCoordinate(double latitude, double longitude)
    {
        if (latitude is < -90 or > 90)
            throw new ArgumentOutOfRangeException(nameof(latitude), latitude, "Latitude must be between -90 and 90.");
        if (longitude is < -180 or > 180)
            throw new ArgumentOutOfRangeException(nameof(longitude), longitude, "Longitude must be between -180 and 180.");

        Latitude = latitude;
        Longitude = longitude;
    }

    /// <summary>
    /// Great-circle distance to <paramref name="other"/> in meters using the Haversine formula.
    /// </summary>
    public double DistanceTo(GeoCoordinate other)
    {
        var lat1 = ToRadians(Latitude);
        var lat2 = ToRadians(other.Latitude);
        var dLat = ToRadians(other.Latitude - Latitude);
        var dLon = ToRadians(other.Longitude - Longitude);

        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
                + Math.Cos(lat1) * Math.Cos(lat2) * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

        return EarthRadiusMeters * c;
    }

    /// <summary>True when <paramref name="other"/> lies within <paramref name="radiusMeters"/> of this point.</summary>
    public bool IsWithin(GeoCoordinate other, double radiusMeters) => DistanceTo(other) <= radiusMeters;

    /// <summary>
    /// Initial bearing (forward azimuth) from this point to <paramref name="other"/>, in degrees [0, 360).
    /// </summary>
    public double InitialBearingTo(GeoCoordinate other)
    {
        var lat1 = ToRadians(Latitude);
        var lat2 = ToRadians(other.Latitude);
        var dLon = ToRadians(other.Longitude - Longitude);

        var y = Math.Sin(dLon) * Math.Cos(lat2);
        var x = Math.Cos(lat1) * Math.Sin(lat2) - Math.Sin(lat1) * Math.Cos(lat2) * Math.Cos(dLon);
        var bearing = Math.Atan2(y, x) * 180d / Math.PI;

        return (bearing + 360d) % 360d;
    }

    /// <summary>Smallest signed difference between two compass headings, in degrees [-180, 180].</summary>
    public static double HeadingDelta(double fromDegrees, double toDegrees)
    {
        var delta = (toDegrees - fromDegrees + 540d) % 360d - 180d;
        return delta;
    }

    private static double ToRadians(double degrees) => degrees * Math.PI / 180d;
}
