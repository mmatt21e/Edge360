using Edge360.Application.Driving;
using Edge360.Domain.Enums;
using FluentAssertions;
using Xunit;

namespace Edge360.Application.Tests;

public class DrivingAnalyzerTests
{
    private static DrivingAnalyzer Sut() => new(new DrivingThresholds());

    [Fact]
    public void Analyze_ShortPath_IsDiscarded()
    {
        // ~100m total — below the 200m minimum trip distance.
        var points = DrivingTestData.BuildPath(DrivingTestData.Constant(10, 11));
        var trips = Sut().Analyze(Guid.NewGuid(), points);
        // 11 points * 10 m/s steps = ~100m -> discarded.
        trips.Should().BeEmpty();
    }

    [Fact]
    public void Analyze_SteadyCruise_ProducesPerfectScore()
    {
        var points = DrivingTestData.BuildPath(DrivingTestData.Constant(20, 30)); // ~580m
        var trips = Sut().Analyze(Guid.NewGuid(), points);

        trips.Should().ContainSingle();
        trips[0].Score.Should().Be(100);
        trips[0].DistanceMeters.Should().BeGreaterThan(200);
        trips[0].MaxSpeedMps.Should().BeApproximately(20, 0.5);
    }

    [Fact]
    public void Analyze_SuddenStop_DetectsHardBraking()
    {
        var speeds = DrivingTestData.Constant(20, 15).Append(0).ToArray();
        var trips = Sut().Analyze(Guid.NewGuid(), DrivingTestData.BuildPath(speeds));

        trips.Should().ContainSingle();
        trips[0].HardBrakingCount.Should().Be(1);
        trips[0].Score.Should().Be(95); // 100 - 5
        trips[0].Events.Should().Contain(e => e.Type == DrivingEventType.HardBraking);
    }

    [Fact]
    public void Analyze_FastStart_DetectsHardAcceleration()
    {
        var speeds = new[] { 0.0 }.Concat(DrivingTestData.Constant(20, 15)).ToArray();
        var trips = Sut().Analyze(Guid.NewGuid(), DrivingTestData.BuildPath(speeds));

        trips.Should().ContainSingle();
        trips[0].HardAccelerationCount.Should().Be(1);
        trips[0].Score.Should().Be(97); // 100 - 3
    }

    [Fact]
    public void Analyze_OverLimit_DetectsSingleSpeedingStretch()
    {
        var trips = Sut().Analyze(Guid.NewGuid(), DrivingTestData.BuildPath(DrivingTestData.Constant(40, 12)));

        trips.Should().ContainSingle();
        trips[0].SpeedingCount.Should().Be(1); // debounced contiguous stretch
        trips[0].Score.Should().Be(96); // 100 - 4
    }

    [Fact]
    public void Analyze_SharpHeadingChange_DetectsCornering()
    {
        var speeds = DrivingTestData.Constant(10, 30); // ~290m, above the 200m minimum
        var headings = new double[30];
        for (var i = 0; i < 30; i++) headings[i] = i < 15 ? 90 : 180; // 90° turn at i=15

        var trips = Sut().Analyze(Guid.NewGuid(), DrivingTestData.BuildPath(speeds, headings: headings));

        trips.Should().ContainSingle();
        trips[0].HarshCorneringCount.Should().BeGreaterThanOrEqualTo(1);
    }

    [Fact]
    public void Analyze_TimeGap_SplitsIntoTwoTrips()
    {
        var first = DrivingTestData.BuildPath(DrivingTestData.Constant(20, 30));
        // Second segment starts 10 minutes after the first ends (> 5 min gap).
        var second = DrivingTestData.BuildPath(
            DrivingTestData.Constant(20, 30),
            start: first[^1].RecordedAt.AddMinutes(10),
            startLon: -73.0);

        var trips = Sut().Analyze(Guid.NewGuid(), first.Concat(second).ToList());

        trips.Should().HaveCount(2);
    }
}
