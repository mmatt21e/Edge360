using Edge360.Application.Geofencing;
using Edge360.Domain.Entities;
using Edge360.Domain.Enums;
using Edge360.Domain.ValueObjects;
using FluentAssertions;
using Xunit;

namespace Edge360.Application.Tests;

public class GeofenceEvaluatorTests
{
    private static Place Home() => new() { Name = "Home", Latitude = 40.0, Longitude = -74.0, RadiusMeters = 150 };

    private static readonly GeoCoordinate Inside = new(40.0, -74.0);
    private static readonly GeoCoordinate Outside = new(41.0, -74.0);

    [Fact]
    public void Evaluate_NoPrevious_ReturnsNoHits()
    {
        var hits = GeofenceEvaluator.Evaluate(null, Inside, new[] { Home() });
        hits.Should().BeEmpty();
    }

    [Fact]
    public void Evaluate_EntersPlace_EmitsArrival()
    {
        var hits = GeofenceEvaluator.Evaluate(Outside, Inside, new[] { Home() });
        hits.Should().ContainSingle()
            .Which.Transition.Should().Be(PlaceTransition.Entered);
    }

    [Fact]
    public void Evaluate_LeavesPlace_EmitsDeparture()
    {
        var hits = GeofenceEvaluator.Evaluate(Inside, Outside, new[] { Home() });
        hits.Should().ContainSingle()
            .Which.Transition.Should().Be(PlaceTransition.Exited);
    }

    [Fact]
    public void Evaluate_ArrivalNotificationsOff_Suppressed()
    {
        var place = Home();
        place.NotifyOnArrival = false;
        var hits = GeofenceEvaluator.Evaluate(Outside, Inside, new[] { place });
        hits.Should().BeEmpty();
    }

    [Fact]
    public void Evaluate_StaysInside_NoTransition()
    {
        var hits = GeofenceEvaluator.Evaluate(Inside, Inside, new[] { Home() });
        hits.Should().BeEmpty();
    }
}
