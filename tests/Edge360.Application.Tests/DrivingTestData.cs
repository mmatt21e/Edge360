using Edge360.Domain.Entities;

namespace Edge360.Application.Tests;

/// <summary>Builds synthetic location streams for driving-analysis tests.</summary>
public static class DrivingTestData
{
    public static readonly DateTimeOffset Base = new(2026, 1, 1, 8, 0, 0, TimeSpan.Zero);

    private static double MetersToLon(double meters, double lat) =>
        meters / (111_320.0 * Math.Cos(lat * Math.PI / 180));

    /// <summary>
    /// Builds an eastbound path, one sample per second, where sample i moves by speeds[i] meters.
    /// Each point carries its speed and (optionally per-point) heading.
    /// </summary>
    public static List<LocationPoint> BuildPath(
        double[] speeds,
        DateTimeOffset? start = null,
        double lat = 40.0,
        double startLon = -74.0,
        double[]? headings = null,
        double defaultHeading = 90)
    {
        var t0 = start ?? Base;
        var points = new List<LocationPoint>();
        var lon = startLon;

        for (var i = 0; i < speeds.Length; i++)
        {
            if (i > 0)
                lon += MetersToLon(speeds[i], lat);

            points.Add(new LocationPoint
            {
                Latitude = lat,
                Longitude = lon,
                SpeedMps = speeds[i],
                Heading = headings is not null ? headings[i] : defaultHeading,
                RecordedAt = t0.AddSeconds(i)
            });
        }

        return points;
    }

    public static double[] Constant(double speed, int count) => Enumerable.Repeat(speed, count).ToArray();
}
