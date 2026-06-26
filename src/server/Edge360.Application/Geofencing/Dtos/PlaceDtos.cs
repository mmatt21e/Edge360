namespace Edge360.Application.Geofencing.Dtos;

public sealed record CreatePlaceRequest(
    Guid GroupId,
    string Name,
    double Latitude,
    double Longitude,
    double RadiusMeters,
    bool NotifyOnArrival = true,
    bool NotifyOnDeparture = true);

public sealed record UpdatePlaceRequest(
    string Name,
    double Latitude,
    double Longitude,
    double RadiusMeters,
    bool NotifyOnArrival,
    bool NotifyOnDeparture);

public sealed record PlaceDto(
    Guid Id,
    Guid GroupId,
    string Name,
    double Latitude,
    double Longitude,
    double RadiusMeters,
    bool NotifyOnArrival,
    bool NotifyOnDeparture);
