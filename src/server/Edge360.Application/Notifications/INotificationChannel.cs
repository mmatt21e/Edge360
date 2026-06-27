namespace Edge360.Application.Notifications;

/// <summary>
/// A delivery transport (email, push/webhook, log). Implementations live in the infrastructure
/// layer; the dispatcher fans a message out to every enabled channel.
/// </summary>
public interface INotificationChannel
{
    /// <summary>Stable channel identifier recorded on the Alert (e.g. "email", "push", "log").</summary>
    string Name { get; }

    /// <summary>Whether this channel is configured and should be used.</summary>
    bool IsEnabled { get; }

    Task<NotificationResult> SendAsync(NotificationMessage message, CancellationToken ct = default);
}

/// <summary>Resolves recipients for an event, delivers via channels, and records Alert rows.</summary>
public interface INotificationDispatcher
{
    Task DispatchEventAsync(Guid eventId, CancellationToken ct = default);
}
