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
