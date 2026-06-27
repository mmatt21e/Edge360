namespace Edge360.Application.Driving.Dtos;

public sealed record AnalyzeDrivingRequest(
    DateTimeOffset? From = null,
    DateTimeOffset? To = null);

public sealed record DrivingEventDto(
    Guid Id,
    string Type,
    DateTimeOffset OccurredAt,
    double Latitude,
    double Longitude,
    double Magnitude);

public sealed record TripDto(
    Guid Id,
    Guid UserId,
    DateTimeOffset StartedAt,
    DateTimeOffset EndedAt,
    double DistanceMeters,
    double DurationSeconds,
    double MaxSpeedMps,
    double AverageSpeedMps,
    int HardBrakingCount,
    int HardAccelerationCount,
    int HarshCorneringCount,
    int SpeedingCount,
    int Score);

/// <summary>A trip with its full event list and ordered route (lat/lon pairs).</summary>
public sealed record TripDetailDto(
    TripDto Trip,
    IReadOnlyList<DrivingEventDto> Events,
    IReadOnlyList<double[]> Route);

/// <summary>Aggregate driving summary over a period.</summary>
public sealed record DrivingScoreDto(
    Guid UserId,
    int TripCount,
    double TotalDistanceMeters,
    int AverageScore,
    int HardBrakingCount,
    int HardAccelerationCount,
    int HarshCorneringCount,
    int SpeedingCount);

public sealed record AnalyzeResultDto(int TripsCreated, IReadOnlyList<TripDto> Trips);
