using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Xunit;

namespace Edge360.Api.IntegrationTests;

public class DrivingEndToEndTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public DrivingEndToEndTests(CustomWebApplicationFactory factory) => _factory = factory;

    private sealed record AuthResponse(string AccessToken, DateTimeOffset AccessTokenExpiresAt, string RefreshToken, object User);
    private sealed record TripDto(Guid Id, Guid UserId, DateTimeOffset StartedAt, DateTimeOffset EndedAt,
        double DistanceMeters, double DurationSeconds, double MaxSpeedMps, double AverageSpeedMps,
        int HardBrakingCount, int HardAccelerationCount, int HarshCorneringCount, int SpeedingCount, int Score);
    private sealed record AnalyzeResult(int TripsCreated, List<TripDto> Trips);
    private sealed record PagedTrips(List<TripDto> Items, int Page, int PageSize, long TotalCount);
    private sealed record ScoreDto(Guid UserId, int TripCount, double TotalDistanceMeters, int AverageScore,
        int HardBrakingCount, int HardAccelerationCount, int HarshCorneringCount, int SpeedingCount);

    private static double MetersToLon(double meters, double lat) => meters / (111_320.0 * Math.Cos(lat * Math.PI / 180));

    [Fact]
    public async Task DrivingJourney_PostTrack_Analyze_ListAndScore()
    {
        var client = _factory.CreateClient();
        var reg = await client.PostAsJsonAsync("/api/auth/register",
            new { email = $"driver-{Guid.NewGuid():N}@x.com", password = "password123", displayName = "Driver" });
        var auth = (await reg.Content.ReadFromJsonAsync<AuthResponse>())!;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);

        // Drive east at 20 m/s for 15s, then a hard stop to 0.
        var speeds = Enumerable.Repeat(20.0, 15).Append(0.0).ToArray();
        const double lat = 40.0;
        var lon = -74.0;
        var t0 = new DateTimeOffset(2026, 3, 1, 9, 0, 0, TimeSpan.Zero);

        for (var i = 0; i < speeds.Length; i++)
        {
            if (i > 0) lon += MetersToLon(speeds[i], lat);
            var resp = await client.PostAsJsonAsync("/api/location",
                new { latitude = lat, longitude = lon, speedMps = speeds[i], heading = 90.0, recordedAt = t0.AddSeconds(i) });
            resp.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        // Analyze into trips.
        var analyzeResp = await client.PostAsJsonAsync("/api/driving/analyze", new { });
        analyzeResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var analyze = (await analyzeResp.Content.ReadFromJsonAsync<AnalyzeResult>())!;
        analyze.TripsCreated.Should().Be(1);
        analyze.Trips[0].HardBrakingCount.Should().Be(1);
        analyze.Trips[0].Score.Should().Be(95);

        // List trips.
        var trips = await client.GetFromJsonAsync<PagedTrips>("/api/driving/trips");
        trips!.Items.Should().ContainSingle();

        // Trip detail has a route.
        var detailResp = await client.GetAsync($"/api/driving/trips/{trips.Items[0].Id}");
        detailResp.StatusCode.Should().Be(HttpStatusCode.OK);

        // Score summary.
        var score = await client.GetFromJsonAsync<ScoreDto>("/api/driving/score");
        score!.TripCount.Should().Be(1);
        score.AverageScore.Should().Be(95);
        score.HardBrakingCount.Should().Be(1);
    }
}
