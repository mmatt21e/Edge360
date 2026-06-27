using Edge360.Web.Models;
using Microsoft.JSInterop;

namespace Edge360.Web.Services;

/// <summary>
/// Map visualization abstraction (spec §10). Wraps the Leaflet JS module so components never
/// touch JS directly; a different provider could implement the same surface.
/// </summary>
public sealed class MapInterop : IAsyncDisposable
{
    private readonly IJSRuntime _js;
    private IJSObjectReference? _module;

    public MapInterop(IJSRuntime js) => _js = js;

    private async Task<IJSObjectReference> ModuleAsync() =>
        _module ??= await _js.InvokeAsync<IJSObjectReference>("import", "./js/map.js");

    public async Task InitAsync(string elementId, double lat = 40.0, double lon = -74.0, int zoom = 12)
    {
        var module = await ModuleAsync();
        await module.InvokeVoidAsync("init", elementId, lat, lon, zoom);
    }

    /// <summary>Replaces all member markers and fits the view to them.</summary>
    public async Task SetMembersAsync(string elementId, IEnumerable<LocationDto> members)
    {
        var module = await ModuleAsync();
        var markers = members.Select(m => new { id = m.UserId, label = m.DisplayName, lat = m.Latitude, lon = m.Longitude });
        await module.InvokeVoidAsync("setMembers", elementId, markers);
    }

    /// <summary>Adds or moves a single member marker (used for live updates).</summary>
    public async Task UpsertMemberAsync(string elementId, LocationDto member)
    {
        var module = await ModuleAsync();
        await module.InvokeVoidAsync("upsertMember", elementId, member.UserId, member.DisplayName, member.Latitude, member.Longitude);
    }

    public async Task SetPlacesAsync(string elementId, IEnumerable<PlaceDto> places)
    {
        var module = await ModuleAsync();
        var circles = places.Select(p => new { id = p.Id, label = p.Name, lat = p.Latitude, lon = p.Longitude, radius = p.RadiusMeters });
        await module.InvokeVoidAsync("setPlaces", elementId, circles);
    }

    public async Task DrawRouteAsync(string elementId, IReadOnlyList<double[]> route)
    {
        var module = await ModuleAsync();
        await module.InvokeVoidAsync("drawRoute", elementId, route);
    }

    /// <summary>Registers a click handler that calls back into <c>OnMapClick(lat, lon)</c> on the ref.</summary>
    public async Task OnClickAsync<T>(string elementId, DotNetObjectReference<T> dotNetRef) where T : class
    {
        var module = await ModuleAsync();
        await module.InvokeVoidAsync("onClick", elementId, dotNetRef);
    }

    public async Task DisposeMapAsync(string elementId)
    {
        if (_module is null) return;
        await _module.InvokeVoidAsync("dispose", elementId);
    }

    public async ValueTask DisposeAsync()
    {
        if (_module is not null)
            await _module.DisposeAsync();
    }
}
