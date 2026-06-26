using Edge360.Application.Common;
using Edge360.Application.Common.Abstractions;
using Edge360.Application.Common.Exceptions;
using Edge360.Application.Geofencing.Dtos;
using Edge360.Domain.Entities;
using Edge360.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Edge360.Application.Geofencing;

/// <summary>CRUD for group places (geofences). Requires Guardian or Admin to mutate.</summary>
public sealed class GeofenceService
{
    private readonly IAppDbContext _db;
    private readonly GroupAccess _access;
    private readonly IAuditWriter _audit;

    public GeofenceService(IAppDbContext db, GroupAccess access, IAuditWriter audit)
    {
        _db = db;
        _access = access;
        _audit = audit;
    }

    public async Task<IReadOnlyList<PlaceDto>> ListAsync(Guid userId, Guid groupId, CancellationToken ct = default)
    {
        await _access.RequireMembershipAsync(groupId, userId, ct);
        return await _db.Places
            .Where(p => p.GroupId == groupId)
            .OrderBy(p => p.Name)
            .Select(p => ToDto(p))
            .ToListAsync(ct);
    }

    public async Task<PlaceDto> CreateAsync(Guid userId, CreatePlaceRequest request, CancellationToken ct = default)
    {
        await _access.RequireRoleAsync(request.GroupId, userId, MemberRole.Guardian, ct);

        var place = new Place
        {
            GroupId = request.GroupId,
            Name = request.Name.Trim(),
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            RadiusMeters = request.RadiusMeters,
            NotifyOnArrival = request.NotifyOnArrival,
            NotifyOnDeparture = request.NotifyOnDeparture
        };
        _db.Places.Add(place);
        await _db.SaveChangesAsync(ct);
        await _audit.WriteAsync("place.create", userId, nameof(Place), place.Id.ToString(), ct: ct);

        return ToDto(place);
    }

    public async Task<PlaceDto> UpdateAsync(Guid userId, Guid placeId, UpdatePlaceRequest request, CancellationToken ct = default)
    {
        var place = await _db.Places.FirstOrDefaultAsync(p => p.Id == placeId, ct)
                    ?? throw NotFoundException.For(nameof(Place), placeId);

        await _access.RequireRoleAsync(place.GroupId, userId, MemberRole.Guardian, ct);

        place.Name = request.Name.Trim();
        place.Latitude = request.Latitude;
        place.Longitude = request.Longitude;
        place.RadiusMeters = request.RadiusMeters;
        place.NotifyOnArrival = request.NotifyOnArrival;
        place.NotifyOnDeparture = request.NotifyOnDeparture;
        place.Touch();

        await _db.SaveChangesAsync(ct);
        await _audit.WriteAsync("place.update", userId, nameof(Place), place.Id.ToString(), ct: ct);

        return ToDto(place);
    }

    public async Task DeleteAsync(Guid userId, Guid placeId, CancellationToken ct = default)
    {
        var place = await _db.Places.FirstOrDefaultAsync(p => p.Id == placeId, ct)
                    ?? throw NotFoundException.For(nameof(Place), placeId);

        await _access.RequireRoleAsync(place.GroupId, userId, MemberRole.Guardian, ct);

        _db.Places.Remove(place);
        await _db.SaveChangesAsync(ct);
        await _audit.WriteAsync("place.delete", userId, nameof(Place), place.Id.ToString(), ct: ct);
    }

    private static PlaceDto ToDto(Place p) => new(
        p.Id, p.GroupId, p.Name, p.Latitude, p.Longitude, p.RadiusMeters, p.NotifyOnArrival, p.NotifyOnDeparture);
}
