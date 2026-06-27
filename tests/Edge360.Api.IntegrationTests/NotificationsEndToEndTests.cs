using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Xunit;

namespace Edge360.Api.IntegrationTests;

public class NotificationsEndToEndTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public NotificationsEndToEndTests(CustomWebApplicationFactory factory) => _factory = factory;

    private sealed record AuthResponse(string AccessToken, DateTimeOffset AccessTokenExpiresAt, string RefreshToken, object User);
    private sealed record GroupDto(Guid Id, string Name, string InviteCode, string Role, int MemberCount, DateTimeOffset CreatedAt);
    private sealed record AlertDto(Guid Id, Guid EventId, string EventType, string Severity, string Message, string Channel, bool Delivered, string? FailureReason, DateTimeOffset OccurredAt);
    private sealed record PagedAlerts(List<AlertDto> Items, int Page, int PageSize, long TotalCount);

    private async Task<HttpClient> RegisterAsync(string email)
    {
        var client = _factory.CreateClient();
        var resp = await client.PostAsJsonAsync("/api/auth/register",
            new { email, password = "password123", displayName = email.Split('@')[0] });
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var auth = (await resp.Content.ReadFromJsonAsync<AuthResponse>())!;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);
        return client;
    }

    [Fact]
    public async Task Sos_DeliversAlertToOtherMember_VisibleInNotifications()
    {
        var owner = await RegisterAsync($"owner-{Guid.NewGuid():N}@x.com");
        var group = (await (await owner.PostAsJsonAsync("/api/groups", new { name = "Alert Fam" }))
            .Content.ReadFromJsonAsync<GroupDto>())!;

        var joiner = await RegisterAsync($"joiner-{Guid.NewGuid():N}@x.com");
        (await joiner.PostAsJsonAsync("/api/groups/join", new { inviteCode = group.InviteCode }))
            .StatusCode.Should().Be(HttpStatusCode.OK);

        // The joiner should have no notifications yet.
        var before = await joiner.GetFromJsonAsync<PagedAlerts>("/api/notifications");
        before!.TotalCount.Should().Be(0);

        // Owner raises an SOS; recipients are all other members (the joiner).
        (await owner.PostAsJsonAsync("/api/events/sos", new { groupId = group.Id, message = "Help!" }))
            .StatusCode.Should().Be(HttpStatusCode.OK);

        var after = await joiner.GetFromJsonAsync<PagedAlerts>("/api/notifications");
        after!.Items.Should().Contain(a => a.EventType == "Sos" && a.Channel == "log" && a.Delivered);

        // The owner (subject) should NOT be notified about their own SOS.
        var ownerAlerts = await owner.GetFromJsonAsync<PagedAlerts>("/api/notifications");
        ownerAlerts!.Items.Should().NotContain(a => a.EventType == "Sos");
    }
}
