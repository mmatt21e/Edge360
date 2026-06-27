using Edge360.Application.Common.Abstractions;
using Edge360.Application.Notifications;
using Edge360.Domain.Entities;
using Edge360.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Edge360.Api.Notifications;

/// <summary>
/// Periodically scans for devices that haven't reported in for longer than the configured window
/// and raises a one-shot OfflineDevice event per group (reset when the device reports again).
/// </summary>
public sealed class OfflineDeviceMonitor : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly NotificationOptions _options;
    private readonly ILogger<OfflineDeviceMonitor> _logger;

    public OfflineDeviceMonitor(IServiceScopeFactory scopeFactory, NotificationOptions options, ILogger<OfflineDeviceMonitor> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = TimeSpan.FromMinutes(Math.Max(1, _options.OfflineScanIntervalMinutes));
        using var timer = new PeriodicTimer(interval);

        while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await ScanAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Offline-device scan failed");
            }
        }
    }

    private async Task ScanAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IAppDbContext>();
        var dispatcher = scope.ServiceProvider.GetRequiredService<INotificationDispatcher>();

        var cutoff = DateTimeOffset.UtcNow - TimeSpan.FromMinutes(_options.OfflineDeviceMinutes);
        var offline = await db.Devices
            .Where(d => d.LastSeenAt != null && d.LastSeenAt < cutoff && !d.OfflineNotified)
            .ToListAsync(ct);

        if (offline.Count == 0)
            return;

        var newEventIds = new List<Guid>();
        foreach (var device in offline)
        {
            device.OfflineNotified = true;

            var user = await db.Users.FirstOrDefaultAsync(u => u.Id == device.UserId, ct);
            var groupIds = await db.Memberships
                .Where(m => m.UserId == device.UserId)
                .Select(m => m.GroupId)
                .ToListAsync(ct);

            foreach (var groupId in groupIds)
            {
                var ev = new SafetyEvent
                {
                    GroupId = groupId,
                    SubjectUserId = device.UserId,
                    Type = EventType.OfflineDevice,
                    Severity = EventSeverity.Warning,
                    Message = $"{user?.DisplayName ?? "A member"}'s device '{device.Name}' appears offline."
                };
                db.Events.Add(ev);
                newEventIds.Add(ev.Id);
            }
        }

        await db.SaveChangesAsync(ct);

        foreach (var id in newEventIds)
            await dispatcher.DispatchEventAsync(id, ct);

        _logger.LogInformation("Raised {Count} offline-device event(s).", newEventIds.Count);
    }
}
