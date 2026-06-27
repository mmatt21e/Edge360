using Edge360.Web.Models;
using Microsoft.AspNetCore.SignalR.Client;

namespace Edge360.Web.Services;

/// <summary>
/// Manages SignalR connections to the location and event hubs, surfacing incoming pushes as
/// C# events for components to subscribe to. The access token is supplied from the token store.
/// </summary>
public sealed class RealtimeService : IAsyncDisposable
{
    private readonly string _apiBase;
    private readonly TokenStore _tokens;

    private HubConnection? _locationHub;
    private HubConnection? _eventHub;

    public RealtimeService(string apiBase, TokenStore tokens)
    {
        _apiBase = apiBase.TrimEnd('/');
        _tokens = tokens;
    }

    public event Action<LocationDto>? LocationUpdated;
    public event Action<EventDto>? EventRaised;

    public bool IsConnected =>
        _locationHub?.State == HubConnectionState.Connected || _eventHub?.State == HubConnectionState.Connected;

    public async Task StartAsync()
    {
        if (_locationHub is not null)
            return;

        _locationHub = Build("/hubs/location");
        _locationHub.On<LocationDto>("locationUpdated", dto => LocationUpdated?.Invoke(dto));

        _eventHub = Build("/hubs/events");
        _eventHub.On<EventDto>("eventRaised", dto => EventRaised?.Invoke(dto));

        await _locationHub.StartAsync();
        await _eventHub.StartAsync();
    }

    private HubConnection Build(string path) =>
        new HubConnectionBuilder()
            .WithUrl($"{_apiBase}{path}", options =>
                options.AccessTokenProvider = async () => await _tokens.GetAccessTokenAsync())
            .WithAutomaticReconnect()
            .Build();

    public async ValueTask DisposeAsync()
    {
        if (_locationHub is not null) await _locationHub.DisposeAsync();
        if (_eventHub is not null) await _eventHub.DisposeAsync();
        _locationHub = null;
        _eventHub = null;
    }
}
