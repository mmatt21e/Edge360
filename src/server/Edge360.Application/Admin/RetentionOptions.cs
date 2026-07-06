namespace Edge360.Application.Admin;

/// <summary>Data-retention policy, bound from configuration section "Retention".</summary>
public sealed class RetentionOptions
{
    public const string SectionName = "Retention";

    /// <summary>When true, the background retention job prunes old data on a schedule.</summary>
    public bool Enabled { get; set; }

    public int LocationRetentionDays { get; set; } = 90;
    public int EventRetentionDays { get; set; } = 180;
    public int AuditRetentionDays { get; set; } = 365;

    /// <summary>How often the background job runs, in hours.</summary>
    public int IntervalHours { get; set; } = 24;
}
