using Edge360.Application.Common.Abstractions;
using Edge360.Domain.Entities;
using Edge360.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Edge360.Application.Notifications;

/// <summary>
/// Resolves recipients for a safety event, delivers the notification across every enabled
/// channel, and records an <see cref="Alert"/> row per (recipient, channel) with the outcome.
/// </summary>
public sealed class NotificationDispatcher : INotificationDispatcher
{
    private readonly IAppDbContext _db;
    private readonly IReadOnlyList<INotificationChannel> _channels;

    public NotificationDispatcher(IAppDbContext db, IEnumerable<INotificationChannel> channels)
    {
        _db = db;
        _channels = channels.ToList();
    }

    public async Task DispatchEventAsync(Guid eventId, CancellationToken ct = default)
    {
        var ev = await _db.Events.FirstOrDefaultAsync(e => e.Id == eventId, ct);
        if (ev is null)
            return;

        var recipientIds = await ResolveRecipientsAsync(ev, ct);
        if (recipientIds.Count == 0)
            return;

        var enabled = _channels.Where(c => c.IsEnabled).ToList();
        if (enabled.Count == 0)
            return;

        var users = await _db.Users
            .Where(u => recipientIds.Contains(u.Id))
            .Select(u => new { u.Id, u.DisplayName, u.Email })
            .ToListAsync(ct);

        var tokensByUser = await _db.Devices
            .Where(d => recipientIds.Contains(d.UserId) && d.PushToken != null)
            .GroupBy(d => d.UserId)
            .Select(g => new { UserId = g.Key, Tokens = g.Select(d => d.PushToken!).ToList() })
            .ToDictionaryAsync(x => x.UserId, x => x.Tokens, ct);

        foreach (var user in users)
        {
            var tokens = tokensByUser.TryGetValue(user.Id, out var t) ? t : new List<string>();
            var message = new NotificationMessage(
                user.Id, user.DisplayName, user.Email, tokens,
                Title: $"{ev.Type}", Body: ev.Message, EventType: ev.Type.ToString(), Severity: ev.Severity.ToString());

            foreach (var channel in enabled)
            {
                NotificationResult result;
                try
                {
                    result = await channel.SendAsync(message, ct);
                }
                catch (Exception ex)
                {
                    result = NotificationResult.Failed(ex.Message);
                }

                if (!result.Attempted)
                    continue;

                _db.Alerts.Add(new Alert
                {
                    EventId = ev.Id,
                    RecipientUserId = user.Id,
                    Channel = channel.Name,
                    Delivered = result.Delivered,
                    DeliveredAt = result.Delivered ? DateTimeOffset.UtcNow : null,
                    FailureReason = result.Error
                });
            }
        }

        await _db.SaveChangesAsync(ct);
    }

    /// <summary>
    /// SOS reaches every other group member; all other event types reach Guardians and Admins only.
    /// The event's subject is never notified about themselves.
    /// </summary>
    private async Task<List<Guid>> ResolveRecipientsAsync(SafetyEvent ev, CancellationToken ct)
    {
        var query = _db.Memberships.Where(m => m.GroupId == ev.GroupId && m.UserId != ev.SubjectUserId);

        if (ev.Type != EventType.Sos)
            query = query.Where(m => m.Role == MemberRole.Admin || m.Role == MemberRole.Guardian);

        return await query.Select(m => m.UserId).Distinct().ToListAsync(ct);
    }
}
