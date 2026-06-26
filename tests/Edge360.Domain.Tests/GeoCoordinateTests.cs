using Edge360.Domain.ValueObjects;
using FluentAssertions;
using Xunit;

namespace Edge360.Domain.Tests;

public class GeoCoordinateTests
{
    [Fact]
    public void DistanceTo_SamePoint_IsZero()
    {
        var p = new GeoCoordinate(40.0, -74.0);
        p.DistanceTo(p).Should().BeApproximately(0, 0.001);
    }

    [Fact]
    public void DistanceTo_KnownPoints_MatchesHaversineWithinTolerance()
    {
        // London (51.5074, -0.1278) to Paris (48.8566, 2.3522) ~ 343 km.
        var london = new GeoCoordinate(51.5074, -0.1278);
        var paris = new GeoCoordinate(48.8566, 2.3522);

        var meters = london.DistanceTo(paris);

        meters.Should().BeInRange(340_000, 345_000);
    }

    [Fact]
    public void IsWithin_InsideRadius_ReturnsTrue()
    {
        var center = new GeoCoordinate(40.0, -74.0);
        var nearby = new GeoCoordinate(40.0009, -74.0); // ~100m north

        center.IsWithin(nearby, 200).Should().BeTrue();
        center.IsWithin(nearby, 50).Should().BeFalse();
    }

    [Theory]
    [InlineData(-91, 0)]
    [InlineData(91, 0)]
    [InlineData(0, -181)]
    [InlineData(0, 181)]
    public void Constructor_OutOfRange_Throws(double lat, double lon)
    {
        var act = () => new GeoCoordinate(lat, lon);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }
}
