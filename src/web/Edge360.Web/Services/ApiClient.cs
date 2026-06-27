using System.Net.Http.Json;
using Edge360.Web.Models;

namespace Edge360.Web.Services;

/// <summary>Typed client for the authenticated API surface.</summary>
public sealed class ApiClient
{
    private readonly HttpClient _http;

    public ApiClient(HttpClient http) => _http = http;

    // Groups
    public Task<List<GroupDto>?> GetGroupsAsync() =>
        _http.GetFromJsonAsync<List<GroupDto>>("/api/groups");

    public async Task<GroupDto> CreateGroupAsync(CreateGroupRequest request) =>
        await PostAsync<GroupDto>("/api/groups", request);

    public async Task<GroupDto> JoinGroupAsync(JoinGroupRequest request) =>
        await PostAsync<GroupDto>("/api/groups/join", request);

    public Task<List<GroupMemberDto>?> GetMembersAsync(Guid groupId) =>
        _http.GetFromJsonAsync<List<GroupMemberDto>>($"/api/groups/{groupId}/members");

    // Location
    public Task<List<LocationDto>?> GetLatestAsync(Guid groupId) =>
        _http.GetFromJsonAsync<List<LocationDto>>($"/api/location/latest?groupId={groupId}");

    public async Task<LocationDto> RecordLocationAsync(RecordLocationRequest request) =>
        await PostAsync<LocationDto>("/api/location", request);

    // Geofences
    public Task<List<PlaceDto>?> GetPlacesAsync(Guid groupId) =>
        _http.GetFromJsonAsync<List<PlaceDto>>($"/api/geofences?groupId={groupId}");

    public async Task<PlaceDto> CreatePlaceAsync(CreatePlaceRequest request) =>
        await PostAsync<PlaceDto>("/api/geofences", request);

    public async Task DeletePlaceAsync(Guid placeId)
    {
        var resp = await _http.DeleteAsync($"/api/geofences/{placeId}");
        await EnsureAsync(resp);
    }

    // Events
    public Task<PagedResult<EventDto>?> GetEventsAsync(Guid groupId, int page = 1, int pageSize = 50) =>
        _http.GetFromJsonAsync<PagedResult<EventDto>>($"/api/events?groupId={groupId}&page={page}&pageSize={pageSize}");

    public async Task<EventDto> RaiseSosAsync(SosRequest request) =>
        await PostAsync<EventDto>("/api/events/sos", request);

    public async Task<EventDto> AcknowledgeEventAsync(Guid eventId) =>
        await PostAsync<EventDto>($"/api/events/{eventId}/acknowledge", new { });

    // Driving
    public Task<PagedResult<TripDto>?> GetTripsAsync(Guid? subjectUserId = null, int page = 1, int pageSize = 50)
    {
        var q = subjectUserId is { } id ? $"?subjectUserId={id}&page={page}&pageSize={pageSize}" : $"?page={page}&pageSize={pageSize}";
        return _http.GetFromJsonAsync<PagedResult<TripDto>>($"/api/driving/trips{q}");
    }

    public async Task<int> AnalyzeDrivingAsync()
    {
        var resp = await _http.PostAsJsonAsync("/api/driving/analyze", new { });
        await EnsureAsync(resp);
        var result = await resp.Content.ReadFromJsonAsync<AnalyzeResult>();
        return result?.TripsCreated ?? 0;
    }

    public Task<TripDetailDto?> GetTripAsync(Guid tripId) =>
        _http.GetFromJsonAsync<TripDetailDto>($"/api/driving/trips/{tripId}");

    public Task<DrivingScoreDto?> GetScoreAsync(Guid? subjectUserId = null)
    {
        var q = subjectUserId is { } id ? $"?subjectUserId={id}" : string.Empty;
        return _http.GetFromJsonAsync<DrivingScoreDto>($"/api/driving/score{q}");
    }

    private async Task<T> PostAsync<T>(string url, object body)
    {
        var resp = await _http.PostAsJsonAsync(url, body);
        await EnsureAsync(resp);
        return (await resp.Content.ReadFromJsonAsync<T>())!;
    }

    private static async Task EnsureAsync(HttpResponseMessage resp)
    {
        if (!resp.IsSuccessStatusCode)
            throw new ApiException(await ProblemReader.ReadMessageAsync(resp));
    }

    private record AnalyzeResult(int TripsCreated);
}

/// <summary>A user-facing API error carrying a friendly message.</summary>
public sealed class ApiException : Exception
{
    public ApiException(string message) : base(message) { }
}
