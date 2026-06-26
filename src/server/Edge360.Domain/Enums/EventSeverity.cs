namespace Edge360.Domain.Enums;

/// <summary>
/// Relative urgency of a safety event, used for notification fan-out and UI emphasis.
/// </summary>
public enum EventSeverity
{
    Info = 0,
    Warning = 1,
    Critical = 2
}
