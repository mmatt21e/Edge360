using Edge360.Application.Notifications;
using Edge360.Domain.Entities;
using Edge360.Domain.Enums;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Edge360.Application.Tests;

public class NotificationDispatcherTests : IDisposable
{
    private readonly SqliteContextFactory _factory = new();

    private Guid _groupId, _subjectId, _guardianId, _memberId;

    public NotificationDispatcherTests()
    {
        using var seed = _factory.Create();
        var subject = new User { Email = "s@x.com", NormalizedEmail = "S@X.COM", DisplayName = "Subject", PasswordHash = "x" };
        var guardian = new User { Email = "g@x.com", NormalizedEmail = "G@X.COM", DisplayName = "Guardian", PasswordHash = "x" };
        var member = new User { Email = "m@x.com", NormalizedEmail = "M@X.COM", DisplayName = "Member", PasswordHash = "x" };
        var group = new Group { Name = "Fam", InviteCode = "CODE1234", CreatedByUserId = guardian.Id };
        seed.AddRange(subject, guardian, member, group,
            new GroupMembership { Group = group, User = subject, Role = MemberRole.Member },
            new GroupMembership { Group = group, User = guardian, Role = MemberRole.Guardian },
            new GroupMembership { Group = group, User = member, Role = MemberRole.Member });
        seed.SaveChanges();
        _groupId = group.Id; _subjectId = subject.Id; _guardianId = guardian.Id; _memberId = member.Id;
    }

    private Guid SeedEvent(EventType type)
    {
        using var ctx = _factory.Create();
        var ev = new SafetyEvent { GroupId = _groupId, SubjectUserId = _subjectId, Type = type, Message = "test" };
        ctx.Events.Add(ev);
        ctx.SaveChanges();
        return ev.Id;
    }

    [Fact]
    public async Task Sos_NotifiesAllOtherMembers()
    {
        var eventId = SeedEvent(EventType.Sos);
        var channel = new FakeChannel("log", enabled: true);

        await using var ctx = _factory.Create();
        await new NotificationDispatcher(ctx, new[] { channel }).DispatchEventAsync(eventId);

        await using var verify = _factory.Create();
        var alerts = await verify.Alerts.Where(a => a.EventId == eventId).ToListAsync();
        alerts.Select(a => a.RecipientUserId).Should().BeEquivalentTo(new[] { _guardianId, _memberId });
        alerts.Should().OnlyContain(a => a.Delivered);
    }

    [Fact]
    public async Task NonSos_NotifiesOnlyGuardiansAndAdmins()
    {
        var eventId = SeedEvent(EventType.Arrival);
        var channel = new FakeChannel("log", enabled: true);

        await using var ctx = _factory.Create();
        await new NotificationDispatcher(ctx, new[] { channel }).DispatchEventAsync(eventId);

        await using var verify = _factory.Create();
        var alerts = await verify.Alerts.Where(a => a.EventId == eventId).ToListAsync();
        alerts.Should().ContainSingle();
        alerts[0].RecipientUserId.Should().Be(_guardianId);
    }

    [Fact]
    public async Task FailingChannel_RecordsUndeliveredAlertWithReason()
    {
        var eventId = SeedEvent(EventType.Sos);
        var channel = new FakeChannel("email", enabled: true, _ => NotificationResult.Failed("smtp down"));

        await using var ctx = _factory.Create();
        await new NotificationDispatcher(ctx, new[] { channel }).DispatchEventAsync(eventId);

        await using var verify = _factory.Create();
        var alerts = await verify.Alerts.Where(a => a.EventId == eventId).ToListAsync();
        alerts.Should().OnlyContain(a => !a.Delivered && a.FailureReason == "smtp down");
    }

    [Fact]
    public async Task SkippedChannel_RecordsNoAlert()
    {
        var eventId = SeedEvent(EventType.Sos);
        var channel = new FakeChannel("email", enabled: true, _ => NotificationResult.Skip());

        await using var ctx = _factory.Create();
        await new NotificationDispatcher(ctx, new[] { channel }).DispatchEventAsync(eventId);

        await using var verify = _factory.Create();
        (await verify.Alerts.CountAsync(a => a.EventId == eventId)).Should().Be(0);
    }

    [Fact]
    public async Task DisabledChannel_IsNotUsed()
    {
        var eventId = SeedEvent(EventType.Sos);
        var enabled = new FakeChannel("log", enabled: true);
        var disabled = new FakeChannel("email", enabled: false);

        await using var ctx = _factory.Create();
        await new NotificationDispatcher(ctx, new INotificationChannel[] { enabled, disabled }).DispatchEventAsync(eventId);

        disabled.Sent.Should().BeEmpty();
        enabled.Sent.Should().HaveCount(2); // guardian + member
    }

    public void Dispose() => _factory.Dispose();
}
