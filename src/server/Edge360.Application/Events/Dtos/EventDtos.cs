namespace Edge360.Application.Events.Dtos;

public sealed record EventDto(
    Guid Id,
    Guid GroupId,
    Guid SubjectUserId,
    string Type,
    string Severity,
    string Message,
    Guid? PlaceId,
    double? Latitude,
    double? Longitude,
    DateTimeOffset OccurredAt,
    bool Acknowledged);

public sealed record SosRequest(
    Guid GroupId,
    double? Latitude = null,
    double? Longitude = null,
    string? Message = null);

public sealed record EventQuery(
    Guid GroupId,
    int Page = 1,
    int PageSize = 50);

/// <summary>A notification delivery record for the current user.</summary>
public sealed record AlertDto(
    Guid Id,
    Guid EventId,
    string EventType,
    string Severity,
    string Message,
    string Channel,
    bool Delivered,
    string? FailureReason,
    DateTimeOffset OccurredAt);
