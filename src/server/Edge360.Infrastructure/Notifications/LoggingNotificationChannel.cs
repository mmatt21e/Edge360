using Edge360.Application.Notifications;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Edge360.Infrastructure.Notifications;

/// <summary>
/// Always-available channel that writes notifications to the structured log. Useful in
/// development and as an audit trail; enabled by default.
/// </summary>
public sealed class LoggingNotificationChannel : INotificationChannel
{
    private readonly ILogger<LoggingNotificationChannel> _logger;
    private readonly LoggingChannelOptions _options;

    public LoggingNotificationChannel(ILogger<LoggingNotificationChannel> logger, IOptions<LoggingChannelOptions> options)
    {
        _logger = logger;
        _options = options.Value;
    }

    public string Name => "log";
    public bool IsEnabled => _options.Enabled;

    public Task<NotificationResult> SendAsync(NotificationMessage message, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "Notification [{Severity}] {EventType} -> {Recipient}: {Body}",
            message.Severity, message.EventType, message.RecipientName, message.Body);
        return Task.FromResult(NotificationResult.Sent());
    }
}
