using Edge360.Application.Common.Abstractions;
using Edge360.Application.Events.Dtos;
using Edge360.Application.Locations.Dtos;
using Microsoft.AspNetCore.SignalR;

namespace Edge360.Api.Realtime;

/// <summary>SignalR implementation of <see cref="IRealtimeNotifier"/> pushing to per-group channels.</summary>
public sealed class SignalRNotifier : IRealtimeNotifier
{
    private readonly IHubContext<LocationHub> _locationHub;
    private readonly IHubContext<EventHub> _eventHub;

    public SignalRNotifier(IHubContext<LocationHub> locationHub, IHubContext<EventHub> eventHub)
    {
        _locationHub = locationHub;
        _eventHub = eventHub;
    }

    public Task LocationUpdatedAsync(Guid groupId, LocationDto location, CancellationToken cancellationToken = default) =>
        _locationHub.Clients.Group(LocationHub.GroupChannel(groupId))
            .SendAsync("locationUpdated", location, cancellationToken);

    public Task EventRaisedAsync(Guid groupId, EventDto safetyEvent, CancellationToken cancellationToken = default) =>
        _eventHub.Clients.Group(EventHub.GroupChannel(groupId))
            .SendAsync("eventRaised", safetyEvent, cancellationToken);
}
