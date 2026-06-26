using Edge360.Domain.Common;
using Edge360.Domain.Enums;

namespace Edge360.Domain.Entities;

/// <summary>
/// A registered device belonging to a user. Source of location samples and push delivery target.
/// </summary>
public class Device : Entity
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public string Name { get; set; } = null!;
    public DevicePlatform Platform { get; set; } = DevicePlatform.Unknown;

    /// <summary>Push notification token (FCM/APNs handle). Transport is abstracted away.</summary>
    public string? PushToken { get; set; }

    public int? BatteryLevel { get; set; }
    public DateTimeOffset? LastSeenAt { get; set; }

    public ICollection<LocationPoint> LocationPoints { get; set; } = new List<LocationPoint>();
}
