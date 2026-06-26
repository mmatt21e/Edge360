using Edge360.Application.Common;
using Edge360.Application.Common.Abstractions;
using Edge360.Application.Common.Models;
using Edge360.Application.Events.Dtos;
using Edge360.Application.Geofencing;
using Edge360.Application.Locations.Dtos;
using Edge360.Domain.Entities;
using Edge360.Domain.Enums;
using Edge360.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace Edge360.Application.Locations;

/// <summary>
/// Ingests location samples, evaluates geofences to raise arrival/departure events,
/// and pushes both location and events to group members in real time.
/// </summary>
public sealed class LocationService
{
    private readonly IAppDbContext _db;
    private readonly GroupAccess _access;
    private readonly IRealtimeNotifier _notifier;

    public LocationService(IAppDbContext db, GroupAccess access, IRealtimeNotifier notifier)
    {
        _db = db;
        _access = access;
        _notifier = notifier;
    }

    public async Task<LocationDto> RecordAsync(Guid userId, RecordLocationRequest request, CancellationToken ct = default)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct)
                   ?? throw new Common.Exceptions.NotFoundException("User not found.");

        // Capture the previous sample before inserting the new one (for geofence transitions).
        var previous = await _db.LocationPoints
            .Where(p => p.UserId == userId)
            .OrderByDescending(p => p.RecordedAt)
            .FirstOrDefaultAsync(ct);

        var point = new LocationPoint
        {
            UserId = userId,
            DeviceId = request.DeviceId,
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            AccuracyMeters = request.AccuracyMeters,
            SpeedMps = request.SpeedMps,
            Heading = request.Heading,
            BatteryLevel = request.BatteryLevel,
            RecordedAt = request.RecordedAt ?? DateTimeOffset.UtcNow
        };
        _db.LocationPoints.Add(point);

        if (request.DeviceId is { } deviceId)
        {
            var device = await _db.Devices.FirstOrDefaultAsync(d => d.Id == deviceId && d.UserId == userId, ct);
            if (device is not null)
            {
                device.LastSeenAt = point.RecordedAt;
                device.BatteryLevel = request.BatteryLevel ?? device.BatteryLevel;
            }
        }

        await _db.SaveChangesAsync(ct);

        var dto = new LocationDto(userId, user.DisplayName, point.Latitude, point.Longitude,
            point.AccuracyMeters, point.SpeedMps, point.BatteryLevel, point.RecordedAt);

        // Fan out to every group where the member is actively sharing.
        var memberships = await _db.Memberships
            .Where(m => m.UserId == userId)
            .ToListAsync(ct);

        var current = point.ToCoordinate();
        var previousCoord = previous is null ? (GeoCoordinate?)null : previous.ToCoordinate();

        foreach (var membership in memberships)
        {
            if (!membership.IsSharingActive)
                continue;

            await _notifier.LocationUpdatedAsync(membership.GroupId, dto, ct);
            await EvaluateGeofencesAsync(membership.GroupId, userId, user.DisplayName, previousCoord, current, ct);
        }

        return dto;
    }

    private async Task EvaluateGeofencesAsync(
        Guid groupId, Guid userId, string displayName,
        GeoCoordinate? previous, GeoCoordinate current, CancellationToken ct)
    {
        var places = await _db.Places.Where(p => p.GroupId == groupId).ToListAsync(ct);
        var hits = GeofenceEvaluator.Evaluate(previous, current, places);
        if (hits.Count == 0)
            return;

        foreach (var hit in hits)
        {
            var type = hit.Transition == PlaceTransition.Entered ? EventType.Arrival : EventType.Departure;
            var verb = hit.Transition == PlaceTransition.Entered ? "arrived at" : "left";

            var safetyEvent = new SafetyEvent
            {
                GroupId = groupId,
                SubjectUserId = userId,
                Type = type,
                Severity = EventSeverity.Info,
                Message = $"{displayName} {verb} {hit.Place.Name}.",
                PlaceId = hit.Place.Id,
                Latitude = current.Latitude,
                Longitude = current.Longitude
            };
            _db.Events.Add(safetyEvent);
            await _db.SaveChangesAsync(ct);

            await _notifier.EventRaisedAsync(groupId, ToEventDto(safetyEvent), ct);
        }
    }

    public async Task<IReadOnlyList<LocationDto>> GetLatestAsync(Guid userId, Guid groupId, CancellationToken ct = default)
    {
        await _access.RequireMembershipAsync(groupId, userId, ct);

        // Latest point per actively-sharing member of the group.
        var sharingMembers = await _db.Memberships
            .Where(m => m.GroupId == groupId && m.LocationSharingEnabled)
            .Select(m => m.UserId)
            .ToListAsync(ct);

        var result = new List<LocationDto>();
        foreach (var memberId in sharingMembers)
        {
            var latest = await _db.LocationPoints
                .Where(p => p.UserId == memberId)
                .OrderByDescending(p => p.RecordedAt)
                .Select(p => new { p.UserId, p.User.DisplayName, p.Latitude, p.Longitude, p.AccuracyMeters, p.SpeedMps, p.BatteryLevel, p.RecordedAt })
                .FirstOrDefaultAsync(ct);

            if (latest is not null)
                result.Add(new LocationDto(latest.UserId, latest.DisplayName, latest.Latitude, latest.Longitude,
                    latest.AccuracyMeters, latest.SpeedMps, latest.BatteryLevel, latest.RecordedAt));
        }

        return result;
    }

    public async Task<PagedResult<LocationDto>> GetHistoryAsync(Guid userId, LocationHistoryQuery query, CancellationToken ct = default)
    {
        await _access.RequireMembershipAsync(query.GroupId, userId, ct);

        // The subject must also be a member of the same group.
        var subjectIsMember = await _db.Memberships
            .AnyAsync(m => m.GroupId == query.GroupId && m.UserId == query.SubjectUserId, ct);
        if (!subjectIsMember)
            throw new Common.Exceptions.NotFoundException("That member is not part of this group.");

        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 1000);

        var baseQuery = _db.LocationPoints.Where(p => p.UserId == query.SubjectUserId);
        if (query.From is { } from) baseQuery = baseQuery.Where(p => p.RecordedAt >= from);
        if (query.To is { } to) baseQuery = baseQuery.Where(p => p.RecordedAt <= to);

        var total = await baseQuery.LongCountAsync(ct);

        var items = await baseQuery
            .OrderByDescending(p => p.RecordedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new LocationDto(p.UserId, p.User.DisplayName, p.Latitude, p.Longitude,
                p.AccuracyMeters, p.SpeedMps, p.BatteryLevel, p.RecordedAt))
            .ToListAsync(ct);

        return new PagedResult<LocationDto>(items, page, pageSize, total);
    }

    internal static EventDto ToEventDto(SafetyEvent e) => new(
        e.Id, e.GroupId, e.SubjectUserId, e.Type.ToString(), e.Severity.ToString(),
        e.Message, e.PlaceId, e.Latitude, e.Longitude, e.OccurredAt, e.Acknowledged);
}
