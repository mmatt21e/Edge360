using Edge360.Application.Locations.Dtos;
using Edge360.Application.Events.Dtos;

namespace Edge360.Application.Common.Abstractions;

/// <summary>
/// Pushes real-time updates to connected clients. Implemented over SignalR in the API layer;
/// the application layer depends only on this abstraction.
/// </summary>
public interface IRealtimeNotifier
{
    /// <summary>Broadcasts a location update to all members of the given group.</summary>
    Task LocationUpdatedAsync(Guid groupId, LocationDto location, CancellationToken cancellationToken = default);

    /// <summary>Broadcasts a safety event to all members of the given group.</summary>
    Task EventRaisedAsync(Guid groupId, EventDto safetyEvent, CancellationToken cancellationToken = default);
}
