namespace Edge360.Web.Models;

// Client-side mirrors of the API contracts. The web app is a pure HTTP client of the API,
// so it defines its own DTOs rather than referencing server projects.

public record RegisterRequest(string Email, string Password, string DisplayName);
public record LoginRequest(string Email, string Password);
public record RefreshRequest(string RefreshToken);
public record UserDto(Guid Id, string Email, string DisplayName, string SystemRole);
public record AuthResponse(string AccessToken, DateTimeOffset AccessTokenExpiresAt, string RefreshToken, UserDto User);

public record CreateGroupRequest(string Name);
public record JoinGroupRequest(string InviteCode);
public record GroupDto(Guid Id, string Name, string InviteCode, string Role, int MemberCount, DateTimeOffset CreatedAt);
public record GroupMemberDto(Guid UserId, string DisplayName, string Email, string Role, bool LocationSharingEnabled, DateTimeOffset JoinedAt);

public record LocationDto(Guid UserId, string DisplayName, double Latitude, double Longitude,
    double? AccuracyMeters, double? SpeedMps, int? BatteryLevel, DateTimeOffset RecordedAt);
public record RecordLocationRequest(double Latitude, double Longitude, double? SpeedMps = null,
    double? Heading = null, int? BatteryLevel = null, DateTimeOffset? RecordedAt = null);

public record PlaceDto(Guid Id, Guid GroupId, string Name, double Latitude, double Longitude,
    double RadiusMeters, bool NotifyOnArrival, bool NotifyOnDeparture);
public record CreatePlaceRequest(Guid GroupId, string Name, double Latitude, double Longitude,
    double RadiusMeters, bool NotifyOnArrival = true, bool NotifyOnDeparture = true);

public record EventDto(Guid Id, Guid GroupId, Guid SubjectUserId, string Type, string Severity, string Message,
    Guid? PlaceId, double? Latitude, double? Longitude, DateTimeOffset OccurredAt, bool Acknowledged);
public record SosRequest(Guid GroupId, double? Latitude = null, double? Longitude = null, string? Message = null);

public record TripDto(Guid Id, Guid UserId, DateTimeOffset StartedAt, DateTimeOffset EndedAt,
    double DistanceMeters, double DurationSeconds, double MaxSpeedMps, double AverageSpeedMps,
    int HardBrakingCount, int HardAccelerationCount, int HarshCorneringCount, int SpeedingCount, int Score);
public record DrivingEventDto(Guid Id, string Type, DateTimeOffset OccurredAt, double Latitude, double Longitude, double Magnitude);
public record TripDetailDto(TripDto Trip, IReadOnlyList<DrivingEventDto> Events, IReadOnlyList<double[]> Route);
public record DrivingScoreDto(Guid UserId, int TripCount, double TotalDistanceMeters, int AverageScore,
    int HardBrakingCount, int HardAccelerationCount, int HarshCorneringCount, int SpeedingCount);

public record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, long TotalCount);
