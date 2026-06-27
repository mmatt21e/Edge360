using Edge360.Domain.Entities;
using Edge360.Domain.Enums;
using Edge360.Domain.ValueObjects;

namespace Edge360.Application.Driving;

/// <summary>
/// Pure driving-analysis engine. Reconstructs trips from an ordered location stream and detects
/// harsh-driving and speeding events, then scores each trip. No I/O — fully unit-testable.
/// </summary>
public sealed class DrivingAnalyzer
{
    private readonly DrivingThresholds _t;

    public DrivingAnalyzer(DrivingThresholds thresholds) => _t = thresholds;

    /// <summary>
    /// Splits the points into trips (on time gaps) and analyzes each. Returns fully-populated
    /// <see cref="Trip"/> entities (with their <see cref="DrivingEvent"/>s) ready to persist.
    /// </summary>
    public IReadOnlyList<Trip> Analyze(Guid userId, IReadOnlyList<LocationPoint> points)
    {
        var ordered = points.OrderBy(p => p.RecordedAt).ToList();
        var trips = new List<Trip>();

        var segment = new List<LocationPoint>();
        for (var i = 0; i < ordered.Count; i++)
        {
            if (segment.Count > 0)
            {
                var gap = (ordered[i].RecordedAt - segment[^1].RecordedAt).TotalMinutes;
                if (gap > _t.TripGapMinutes)
                {
                    BuildTrip(userId, segment, trips);
                    segment = new List<LocationPoint>();
                }
            }
            segment.Add(ordered[i]);
        }
        BuildTrip(userId, segment, trips);

        return trips;
    }

    private void BuildTrip(Guid userId, List<LocationPoint> segment, List<Trip> trips)
    {
        if (segment.Count < 2)
            return;

        var n = segment.Count;
        var speeds = new double[n];
        var headings = new double[n];

        // Precompute per-point speed (reported, else derived) and heading (reported, else bearing).
        speeds[0] = segment[0].SpeedMps ?? 0;
        headings[0] = segment[0].Heading ?? 0;
        for (var i = 1; i < n; i++)
        {
            var dt = (segment[i].RecordedAt - segment[i - 1].RecordedAt).TotalSeconds;
            var d = segment[i - 1].ToCoordinate().DistanceTo(segment[i].ToCoordinate());
            speeds[i] = segment[i].SpeedMps ?? (dt > 0 ? d / dt : 0);
            headings[i] = segment[i].Heading ?? segment[i - 1].ToCoordinate().InitialBearingTo(segment[i].ToCoordinate());
        }

        double distance = 0;
        double maxSpeed = speeds.Max();
        var events = new List<DrivingEvent>();

        var lastEventAt = new Dictionary<DrivingEventType, DateTimeOffset>();
        var speedingActive = false;

        for (var i = 1; i < n; i++)
        {
            var a = segment[i - 1];
            var b = segment[i];
            var dt = (b.RecordedAt - a.RecordedAt).TotalSeconds;
            if (dt <= 0)
                continue;

            distance += a.ToCoordinate().DistanceTo(b.ToCoordinate());

            var accel = (speeds[i] - speeds[i - 1]) / dt;

            if (accel <= _t.HardBrakingMps2)
                TryAddEvent(events, lastEventAt, DrivingEventType.HardBraking, b, Math.Abs(accel));
            else if (accel >= _t.HardAccelerationMps2)
                TryAddEvent(events, lastEventAt, DrivingEventType.HardAcceleration, b, accel);

            if (speeds[i] >= _t.MinMovingSpeedMps)
            {
                var headingRate = Math.Abs(GeoCoordinate.HeadingDelta(headings[i - 1], headings[i])) / dt;
                if (headingRate >= _t.HarshCorneringDegPerSec)
                    TryAddEvent(events, lastEventAt, DrivingEventType.HarshCornering, b, headingRate);
            }

            // Speeding is debounced as a contiguous over-limit stretch (hysteresis at 95%).
            if (!speedingActive && speeds[i] > _t.SpeedingLimitMps)
            {
                events.Add(NewEvent(DrivingEventType.Speeding, b, speeds[i]));
                speedingActive = true;
            }
            else if (speedingActive && speeds[i] < _t.SpeedingLimitMps * 0.95)
            {
                speedingActive = false;
            }
        }

        if (distance < _t.MinTripDistanceMeters)
            return;

        var duration = (segment[^1].RecordedAt - segment[0].RecordedAt).TotalSeconds;

        var trip = new Trip
        {
            UserId = userId,
            StartedAt = segment[0].RecordedAt,
            EndedAt = segment[^1].RecordedAt,
            DistanceMeters = distance,
            DurationSeconds = duration,
            MaxSpeedMps = maxSpeed,
            AverageSpeedMps = duration > 0 ? distance / duration : 0,
            HardBrakingCount = events.Count(e => e.Type == DrivingEventType.HardBraking),
            HardAccelerationCount = events.Count(e => e.Type == DrivingEventType.HardAcceleration),
            HarshCorneringCount = events.Count(e => e.Type == DrivingEventType.HarshCornering),
            SpeedingCount = events.Count(e => e.Type == DrivingEventType.Speeding)
        };

        foreach (var e in events)
        {
            e.UserId = userId;
            trip.Events.Add(e);
        }

        trip.Score = ComputeScore(trip);
        trips.Add(trip);
    }

    private int ComputeScore(Trip trip)
    {
        var penalty = trip.HardBrakingCount * _t.HardBrakingPenalty
                      + trip.HardAccelerationCount * _t.HardAccelerationPenalty
                      + trip.HarshCorneringCount * _t.HarshCorneringPenalty
                      + trip.SpeedingCount * _t.SpeedingPenalty;

        return Math.Clamp(100 - penalty, 0, 100);
    }

    private void TryAddEvent(
        List<DrivingEvent> events,
        Dictionary<DrivingEventType, DateTimeOffset> lastEventAt,
        DrivingEventType type,
        LocationPoint at,
        double magnitude)
    {
        if (lastEventAt.TryGetValue(type, out var last)
            && (at.RecordedAt - last).TotalSeconds < _t.EventCooldownSeconds)
            return;

        events.Add(NewEvent(type, at, magnitude));
        lastEventAt[type] = at.RecordedAt;
    }

    private static DrivingEvent NewEvent(DrivingEventType type, LocationPoint at, double magnitude) => new()
    {
        Type = type,
        OccurredAt = at.RecordedAt,
        Latitude = at.Latitude,
        Longitude = at.Longitude,
        Magnitude = magnitude
    };
}
