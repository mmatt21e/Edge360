using Edge360.Application.Common;
using Edge360.Application.Common.Exceptions;
using Edge360.Application.Driving;
using Edge360.Application.Driving.Dtos;
using Edge360.Domain.Entities;
using Edge360.Domain.Enums;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Edge360.Application.Tests;

public class DrivingServiceTests : IDisposable
{
    private readonly SqliteContextFactory _factory = new();

    private DrivingService CreateSut()
    {
        var ctx = _factory.Create();
        return new DrivingService(ctx, new GroupAccess(ctx), new DrivingAnalyzer(new DrivingThresholds()), new NullAuditWriter());
    }

    private Guid SeedUserWithBrakingTrip()
    {
        using var seed = _factory.Create();
        var user = new User { Email = "d@x.com", NormalizedEmail = "D@X.COM", DisplayName = "Driver", PasswordHash = "x" };
        seed.Users.Add(user);

        var speeds = DrivingTestData.Constant(20, 15).Append(0).ToArray();
        foreach (var p in DrivingTestData.BuildPath(speeds))
        {
            p.UserId = user.Id;
            seed.LocationPoints.Add(p);
        }
        seed.SaveChanges();
        return user.Id;
    }

    [Fact]
    public async Task Analyze_PersistsTripsAndEvents()
    {
        var userId = SeedUserWithBrakingTrip();

        var result = await CreateSut().AnalyzeAsync(userId, new AnalyzeDrivingRequest());

        result.TripsCreated.Should().Be(1);

        await using var verify = _factory.Create();
        (await verify.Trips.CountAsync()).Should().Be(1);
        (await verify.DrivingEvents.CountAsync(e => e.Type == DrivingEventType.HardBraking)).Should().Be(1);
    }

    [Fact]
    public async Task Analyze_IsIdempotent_ReplacesPriorTrips()
    {
        var userId = SeedUserWithBrakingTrip();

        await CreateSut().AnalyzeAsync(userId, new AnalyzeDrivingRequest());
        await CreateSut().AnalyzeAsync(userId, new AnalyzeDrivingRequest());

        await using var verify = _factory.Create();
        (await verify.Trips.CountAsync()).Should().Be(1); // not duplicated
    }

    [Fact]
    public async Task ListAndScore_ReflectAnalyzedTrip()
    {
        var userId = SeedUserWithBrakingTrip();
        await CreateSut().AnalyzeAsync(userId, new AnalyzeDrivingRequest());

        var trips = await CreateSut().ListTripsAsync(userId, null, null, null, 1, 50);
        trips.Items.Should().ContainSingle();
        trips.Items[0].HardBrakingCount.Should().Be(1);

        var score = await CreateSut().GetScoreSummaryAsync(userId, null, null, null);
        score.TripCount.Should().Be(1);
        score.AverageScore.Should().Be(95);
        score.HardBrakingCount.Should().Be(1);
    }

    [Fact]
    public async Task GetTrip_IncludesRouteAndEvents()
    {
        var userId = SeedUserWithBrakingTrip();
        await CreateSut().AnalyzeAsync(userId, new AnalyzeDrivingRequest());
        var listed = await CreateSut().ListTripsAsync(userId, null, null, null, 1, 50);

        var detail = await CreateSut().GetTripAsync(userId, listed.Items[0].Id);

        detail.Route.Should().NotBeEmpty();
        detail.Events.Should().Contain(e => e.Type == nameof(DrivingEventType.HardBraking));
    }

    [Fact]
    public async Task ListTrips_ForUnrelatedUser_IsForbidden()
    {
        var driverId = SeedUserWithBrakingTrip();

        // A stranger who shares no group must not view the driver's trips.
        Guid strangerId;
        using (var seed = _factory.Create())
        {
            var stranger = new User { Email = "s@x.com", NormalizedEmail = "S@X.COM", DisplayName = "Stranger", PasswordHash = "x" };
            seed.Users.Add(stranger);
            seed.SaveChanges();
            strangerId = stranger.Id;
        }

        var act = async () => await CreateSut().ListTripsAsync(strangerId, driverId, null, null, 1, 50);
        await act.Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task ListTrips_ForGroupMember_IsAllowed()
    {
        var driverId = SeedUserWithBrakingTrip();
        await CreateSut().AnalyzeAsync(driverId, new AnalyzeDrivingRequest());

        // A guardian sharing a group with the driver may view their trips.
        Guid guardianId;
        using (var seed = _factory.Create())
        {
            var guardian = new User { Email = "g@x.com", NormalizedEmail = "G@X.COM", DisplayName = "Guardian", PasswordHash = "x" };
            var group = new Group { Name = "Fam", InviteCode = "JOIN1234", CreatedByUserId = guardian.Id };
            seed.AddRange(
                guardian, group,
                new GroupMembership { Group = group, User = guardian, Role = MemberRole.Guardian },
                new GroupMembership { Group = group, UserId = driverId, Role = MemberRole.Member });
            seed.SaveChanges();
            guardianId = guardian.Id;
        }

        var trips = await CreateSut().ListTripsAsync(guardianId, driverId, null, null, 1, 50);
        trips.Items.Should().ContainSingle();
    }

    public void Dispose() => _factory.Dispose();
}
