using Edge360.Domain.Entities;
using Edge360.Domain.Enums;
using Edge360.Domain.ValueObjects;

namespace Edge360.Application.Geofencing;

/// <summary>A place whose boundary was crossed between two location samples.</summary>
public sealed record GeofenceHit(Place Place, PlaceTransition Transition);

/// <summary>
/// Pure geofence transition logic: compares a member's previous and current positions
/// against a set of places. No I/O, so it is trivially unit-testable.
/// </summary>
public static class GeofenceEvaluator
{
    /// <summary>
    /// Returns the arrival/departure transitions triggered by moving from
    /// <paramref name="previous"/> to <paramref name="current"/>. When there is no previous
    /// sample, no transitions are produced (we cannot infer a crossing from a single point).
    /// </summary>
    public static IReadOnlyList<GeofenceHit> Evaluate(
        GeoCoordinate? previous,
        GeoCoordinate current,
        IEnumerable<Place> places)
    {
        var hits = new List<GeofenceHit>();
        if (previous is null)
            return hits;

        foreach (var place in places)
        {
            var wasInside = place.Contains(previous.Value);
            var transition = place.Evaluate(wasInside, current);

            if (transition == PlaceTransition.Entered && place.NotifyOnArrival)
                hits.Add(new GeofenceHit(place, transition));
            else if (transition == PlaceTransition.Exited && place.NotifyOnDeparture)
                hits.Add(new GeofenceHit(place, transition));
        }

        return hits;
    }
}
