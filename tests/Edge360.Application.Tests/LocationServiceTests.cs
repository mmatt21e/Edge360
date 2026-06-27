using Edge360.Application.Common;
using Edge360.Application.Locations;
using Edge360.Application.Locations.Dtos;
using Edge360.Domain.Entities;
using Edge360.Domain.Enums;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Edge360.Application.Tests;

public class LocationServiceTests : IDisposable
{
    private readonly SqliteContextFactory _factory = new();
    private readonly RecordingNotifier _notifier = new();

    private LocationService CreateSut(out Guid userId, out Guid groupId)
    {
        using (var seed = _factory.Create())
        {
            var user = new User { Email = "u@x.com", NormalizedEmail = "U@X.COM", DisplayName = "Unit", PasswordHash = "x" };
            var group = new Group { Name = "Fam", InviteCode = "ABCD1234", CreatedByUserId = user.Id };
            var membership = new GroupMembership { Group = group, User = user, Role = MemberRole.Admin };
            var place = new Place { Group = group, Name = "Home", Latitude = 40.0, Longitude = -74.0, RadiusMeters = 150 };

            seed.AddRange(user, group, membership, place);
            seed.SaveChanges();

            userId = user.Id;
            groupId = group.Id;
        }

        var ctx = _factory.Create();
        return new LocationService(ctx, new GroupAccess(ctx), _notifier,
            new RecordingDispatcher(), new Edge360.Application.Notifications.NotificationOptions());
    }

    [Fact]
    public async Task Record_FirstThenArrival_RaisesArrivalEvent()
    {
        var sut = CreateSut(out var userId, out var groupId);

        // First sample outside the geofence (bootstrap: no previous point => no event).
        await sut.RecordAsync(userId, new RecordLocationRequest(41.0, -74.0));
        // Second sample inside the geofence => arrival.
        await sut.RecordAsync(userId, new RecordLocationRequest(40.0, -74.0));

        await using var verify = _factory.Create();
        var events = await verify.Events.ToListAsync();
        events.Should().ContainSingle();
        events[0].Type.Should().Be(EventType.Arrival);
        events[0].GroupId.Should().Be(groupId);

        _notifier.Events.Should().ContainSingle(e => e.Event.Type == nameof(EventType.Arrival));
        _notifier.Locations.Should().HaveCount(2); // both samples broadcast
    }

    [Fact]
    public async Task Record_NoMovementAcrossBoundary_RaisesNoEvent()
    {
        var sut = CreateSut(out var userId, out _);

        await sut.RecordAsync(userId, new RecordLocationRequest(41.0, -74.0));
        await sut.RecordAsync(userId, new RecordLocationRequest(41.1, -74.0)); // still outside

        await using var verify = _factory.Create();
        (await verify.Events.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task GetLatest_ReturnsMostRecentPerMember()
    {
        var sut = CreateSut(out var userId, out var groupId);

        var t0 = DateTimeOffset.UtcNow.AddMinutes(-5);
        await sut.RecordAsync(userId, new RecordLocationRequest(40.0, -74.0, RecordedAt: t0));
        await sut.RecordAsync(userId, new RecordLocationRequest(40.5, -74.5, RecordedAt: t0.AddMinutes(1)));

        var latest = await sut.GetLatestAsync(userId, groupId);

        latest.Should().ContainSingle();
        latest[0].Latitude.Should().Be(40.5);
    }

    [Fact]
    public async Task Record_BatteryCrossesThreshold_RaisesSingleLowBatteryEvent()
    {
        var sut = CreateSut(out var userId, out _);
        var t = DateTimeOffset.UtcNow.AddMinutes(-10);

        await sut.RecordAsync(userId, new RecordLocationRequest(41.0, -74.0, BatteryLevel: 80, RecordedAt: t));
        await sut.RecordAsync(userId, new RecordLocationRequest(41.0, -74.0, BatteryLevel: 10, RecordedAt: t.AddMinutes(1)));
        await sut.RecordAsync(userId, new RecordLocationRequest(41.0, -74.0, BatteryLevel: 8, RecordedAt: t.AddMinutes(2)));

        await using var verify = _factory.Create();
        var lowBattery = await verify.Events.Where(e => e.Type == EventType.LowBattery).ToListAsync();
        lowBattery.Should().ContainSingle(); // raised once on crossing, debounced after
        lowBattery[0].Severity.Should().Be(EventSeverity.Warning);
    }

    public void Dispose() => _factory.Dispose();
}
