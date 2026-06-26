using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Xunit;

namespace Edge360.Api.IntegrationTests;

public class EndToEndTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public EndToEndTests(CustomWebApplicationFactory factory) => _factory = factory;

    private sealed record AuthResponse(string AccessToken, DateTimeOffset AccessTokenExpiresAt, string RefreshToken, UserDto User);
    private sealed record UserDto(Guid Id, string Email, string DisplayName, string SystemRole);
    private sealed record GroupDto(Guid Id, string Name, string InviteCode, string Role, int MemberCount, DateTimeOffset CreatedAt);
    private sealed record PlaceDto(Guid Id, Guid GroupId, string Name, double Latitude, double Longitude, double RadiusMeters, bool NotifyOnArrival, bool NotifyOnDeparture);
    private sealed record EventDto(Guid Id, Guid GroupId, Guid SubjectUserId, string Type, string Severity, string Message, Guid? PlaceId, double? Latitude, double? Longitude, DateTimeOffset OccurredAt, bool Acknowledged);
    private sealed record PagedEvents(List<EventDto> Items, int Page, int PageSize, long TotalCount);

    private async Task<(HttpClient client, AuthResponse auth)> RegisterClientAsync(string email)
    {
        var client = _factory.CreateClient();
        var resp = await client.PostAsJsonAsync("/api/auth/register",
            new { email, password = "password123", displayName = "Tester" });
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var auth = (await resp.Content.ReadFromJsonAsync<AuthResponse>())!;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);
        return (client, auth);
    }

    [Fact]
    public async Task Health_ReturnsHealthy()
    {
        var client = _factory.CreateClient();
        var resp = await client.GetAsync("/health");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Register_ThenAccessProtectedEndpoint_Works()
    {
        var (client, _) = await RegisterClientAsync($"flow-{Guid.NewGuid():N}@x.com");

        var groups = await client.GetAsync("/api/groups");
        groups.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ProtectedEndpoint_WithoutToken_Returns401()
    {
        var client = _factory.CreateClient();
        var resp = await client.GetAsync("/api/groups");
        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Register_DuplicateEmail_Returns409()
    {
        var email = $"dupe-{Guid.NewGuid():N}@x.com";
        var (client, _) = await RegisterClientAsync(email);

        var second = await client.PostAsJsonAsync("/api/auth/register",
            new { email, password = "password123", displayName = "Again" });

        second.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Register_InvalidPayload_Returns400()
    {
        var client = _factory.CreateClient();
        var resp = await client.PostAsJsonAsync("/api/auth/register",
            new { email = "not-an-email", password = "short", displayName = "" });

        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task FullJourney_CreateGroup_Place_Location_RaisesArrivalEvent()
    {
        var (client, _) = await RegisterClientAsync($"journey-{Guid.NewGuid():N}@x.com");

        // Create a group.
        var groupResp = await client.PostAsJsonAsync("/api/groups", new { name = "My Family" });
        groupResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var group = (await groupResp.Content.ReadFromJsonAsync<GroupDto>())!;
        group.Role.Should().Be("Admin");

        // Define a geofence ("Home").
        var placeResp = await client.PostAsJsonAsync("/api/geofences", new
        {
            groupId = group.Id,
            name = "Home",
            latitude = 40.0,
            longitude = -74.0,
            radiusMeters = 150.0
        });
        placeResp.StatusCode.Should().Be(HttpStatusCode.OK);
        (await placeResp.Content.ReadFromJsonAsync<PlaceDto>())!.Name.Should().Be("Home");

        // First location outside (bootstrap), then inside -> arrival event.
        (await client.PostAsJsonAsync("/api/location", new { latitude = 41.0, longitude = -74.0 }))
            .StatusCode.Should().Be(HttpStatusCode.OK);
        (await client.PostAsJsonAsync("/api/location", new { latitude = 40.0, longitude = -74.0 }))
            .StatusCode.Should().Be(HttpStatusCode.OK);

        // Latest location is visible.
        var latest = await client.GetFromJsonAsync<List<Dictionary<string, object>>>(
            $"/api/location/latest?groupId={group.Id}");
        latest.Should().NotBeNull();
        latest!.Should().ContainSingle();

        // The arrival event was recorded.
        var events = await client.GetFromJsonAsync<PagedEvents>($"/api/events?groupId={group.Id}");
        events!.Items.Should().Contain(e => e.Type == "Arrival");

        // SOS raises a critical event.
        var sos = await client.PostAsJsonAsync("/api/events/sos", new { groupId = group.Id, message = "Help" });
        sos.StatusCode.Should().Be(HttpStatusCode.OK);
        (await sos.Content.ReadFromJsonAsync<EventDto>())!.Severity.Should().Be("Critical");
    }

    [Fact]
    public async Task JoinGroup_WithInviteCode_AddsSecondMember()
    {
        var (owner, _) = await RegisterClientAsync($"owner-{Guid.NewGuid():N}@x.com");
        var groupResp = await owner.PostAsJsonAsync("/api/groups", new { name = "Shared" });
        var group = (await groupResp.Content.ReadFromJsonAsync<GroupDto>())!;

        var (joiner, _) = await RegisterClientAsync($"joiner-{Guid.NewGuid():N}@x.com");
        var joinResp = await joiner.PostAsJsonAsync("/api/groups/join", new { inviteCode = group.InviteCode });
        joinResp.StatusCode.Should().Be(HttpStatusCode.OK);

        var members = await owner.GetFromJsonAsync<List<Dictionary<string, object>>>($"/api/groups/{group.Id}/members");
        members!.Should().HaveCount(2);
    }
}
