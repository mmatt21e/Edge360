namespace Edge360.Domain.Enums;

/// <summary>
/// Result of evaluating a member's position against a geofenced place between two samples.
/// </summary>
public enum PlaceTransition
{
    /// <summary>No boundary crossing relative to the previous known state.</summary>
    None = 0,

    /// <summary>Member moved from outside to inside the place radius.</summary>
    Entered = 1,

    /// <summary>Member moved from inside to outside the place radius.</summary>
    Exited = 2
}
