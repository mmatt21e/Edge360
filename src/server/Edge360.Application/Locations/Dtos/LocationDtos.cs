namespace Edge360.Application.Locations.Dtos;

public sealed record RecordLocationRequest(
    double Latitude,
    double Longitude,
    double? AccuracyMeters = null,
    double? SpeedMps = null,
    double? Heading = null,
    int? BatteryLevel = null,
    Guid? DeviceId = null,
    DateTimeOffset? RecordedAt = null);

public sealed record LocationDto(
    Guid UserId,
    string DisplayName,
    double Latitude,
    double Longitude,
    double? AccuracyMeters,
    double? SpeedMps,
    int? BatteryLevel,
    DateTimeOffset RecordedAt);

public sealed record LocationHistoryQuery(
    Guid GroupId,
    Guid SubjectUserId,
    DateTimeOffset? From = null,
    DateTimeOffset? To = null,
    int Page = 1,
    int PageSize = 100);
