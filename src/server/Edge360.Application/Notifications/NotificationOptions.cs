namespace Edge360.Application.Notifications;

/// <summary>General notification behavior, bound from configuration section "Notifications".</summary>
public sealed class NotificationOptions
{
    public const string SectionName = "Notifications";

    /// <summary>Battery percentage at or below which a low-battery alert is raised.</summary>
    public int LowBatteryThresholdPercent { get; set; } = 15;

    /// <summary>A device unseen for this many minutes is considered offline.</summary>
    public int OfflineDeviceMinutes { get; set; } = 30;

    /// <summary>How often the offline-device monitor scans, in minutes.</summary>
    public int OfflineScanIntervalMinutes { get; set; } = 5;
}
