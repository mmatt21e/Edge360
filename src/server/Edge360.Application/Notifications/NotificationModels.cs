namespace Edge360.Application.Notifications;

/// <summary>A notification ready to be delivered to one recipient over one or more channels.</summary>
public sealed record NotificationMessage(
    Guid RecipientUserId,
    string RecipientName,
    string? Email,
    IReadOnlyList<string> PushTokens,
    string Title,
    string Body,
    string EventType,
    string Severity);

/// <summary>Outcome of a single channel send attempt.</summary>
public sealed record NotificationResult(bool Attempted, bool Delivered, string? Error)
{
    public static NotificationResult Sent() => new(true, true, null);
    public static NotificationResult Failed(string error) => new(true, false, error);

    /// <summary>The channel chose not to handle this message (e.g. no address); no Alert is recorded.</summary>
    public static NotificationResult Skip() => new(false, false, null);
}
