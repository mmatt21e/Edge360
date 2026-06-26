namespace Edge360.Domain.Enums;

/// <summary>
/// Categories of safety/activity events surfaced to guardians.
/// </summary>
public enum EventType
{
    Arrival = 0,
    Departure = 1,
    Sos = 2,
    LowBattery = 3,
    OfflineDevice = 4,
    Speeding = 5,
    CrashSuspected = 6
}
