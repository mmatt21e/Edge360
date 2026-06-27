namespace Edge360.Infrastructure.Notifications;

/// <summary>SMTP email channel settings, bound from configuration section "Email".</summary>
public sealed class EmailOptions
{
    public const string SectionName = "Email";

    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 587;
    public bool UseSsl { get; set; } = true;
    public string? Username { get; set; }
    public string? Password { get; set; }
    public string FromAddress { get; set; } = "no-reply@edge360.local";
    public string FromName { get; set; } = "Edge360";

    public bool IsConfigured => !string.IsNullOrWhiteSpace(Host);
}

/// <summary>
/// Webhook (push) channel settings, bound from configuration section "Webhook". POSTs a JSON
/// payload to a configurable URL — works with self-hosted push gateways (ntfy, Gotify, custom).
/// </summary>
public sealed class WebhookOptions
{
    public const string SectionName = "Webhook";

    public string Url { get; set; } = string.Empty;

    /// <summary>Optional bearer/authorization header value sent with each request.</summary>
    public string? AuthorizationHeader { get; set; }

    public bool IsConfigured => !string.IsNullOrWhiteSpace(Url);
}

/// <summary>Logging channel toggle, bound from configuration section "LoggingNotifications".</summary>
public sealed class LoggingChannelOptions
{
    public const string SectionName = "LoggingNotifications";

    /// <summary>When true (default), notifications are always written to the structured log.</summary>
    public bool Enabled { get; set; } = true;
}
