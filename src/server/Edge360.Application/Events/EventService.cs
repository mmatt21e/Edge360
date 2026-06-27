using Edge360.Application.Common;
using Edge360.Application.Common.Abstractions;
using Edge360.Application.Common.Exceptions;
using Edge360.Application.Common.Models;
using Edge360.Application.Events.Dtos;
using Edge360.Application.Notifications;
using Edge360.Domain.Entities;
using Edge360.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Edge360.Application.Events;

/// <summary>Reads the group activity feed, raises SOS events, and acknowledges events.</summary>
public sealed class EventService
{
    private readonly IAppDbContext _db;
    private readonly GroupAccess _access;
    private readonly IRealtimeNotifier _notifier;
    private readonly INotificationDispatcher _dispatcher;
    private readonly IAuditWriter _audit;

    public EventService(IAppDbContext db, GroupAccess access, IRealtimeNotifier notifier,
        INotificationDispatcher dispatcher, IAuditWriter audit)
    {
        _db = db;
        _access = access;
        _notifier = notifier;
        _dispatcher = dispatcher;
        _audit = audit;
    }

    public async Task<PagedResult<EventDto>> ListAsync(Guid userId, EventQuery query, CancellationToken ct = default)
    {
        await _access.RequireMembershipAsync(query.GroupId, userId, ct);

        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 200);

        var baseQuery = _db.Events.Where(e => e.GroupId == query.GroupId);
        var total = await baseQuery.LongCountAsync(ct);

        var items = await baseQuery
            .OrderByDescending(e => e.OccurredAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(e => ToDto(e))
            .ToListAsync(ct);

        return new PagedResult<EventDto>(items, page, pageSize, total);
    }

    public async Task<EventDto> RaiseSosAsync(Guid userId, SosRequest request, CancellationToken ct = default)
    {
        await _access.RequireMembershipAsync(request.GroupId, userId, ct);

        var user = await _db.Users.FirstAsync(u => u.Id == userId, ct);
        var safetyEvent = new SafetyEvent
        {
            GroupId = request.GroupId,
            SubjectUserId = userId,
            Type = EventType.Sos,
            Severity = EventSeverity.Critical,
            Message = string.IsNullOrWhiteSpace(request.Message)
                ? $"{user.DisplayName} triggered an SOS."
                : $"{user.DisplayName}: {request.Message!.Trim()}",
            Latitude = request.Latitude,
            Longitude = request.Longitude
        };

        _db.Events.Add(safetyEvent);
        await _db.SaveChangesAsync(ct);
        await _audit.WriteAsync("event.sos", userId, nameof(SafetyEvent), safetyEvent.Id.ToString(), ct: ct);

        var dto = ToDto(safetyEvent);
        await _notifier.EventRaisedAsync(request.GroupId, dto, ct);

        // Deliver to recipients across all enabled channels (records Alert rows with outcomes).
        await _dispatcher.DispatchEventAsync(safetyEvent.Id, ct);
        return dto;
    }

    /// <summary>Lists notification (alert) records addressed to the caller, newest first.</summary>
    public async Task<PagedResult<AlertDto>> ListMyAlertsAsync(Guid userId, int page, int pageSize, CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 200);

        var baseQuery = _db.Alerts.Where(a => a.RecipientUserId == userId);
        var total = await baseQuery.LongCountAsync(ct);

        var items = await baseQuery
            .OrderByDescending(a => a.Event.OccurredAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new AlertDto(
                a.Id, a.EventId, a.Event.Type.ToString(), a.Event.Severity.ToString(),
                a.Event.Message, a.Channel, a.Delivered, a.FailureReason, a.Event.OccurredAt))
            .ToListAsync(ct);

        return new PagedResult<AlertDto>(items, page, pageSize, total);
    }

    public async Task<EventDto> AcknowledgeAsync(Guid userId, Guid eventId, CancellationToken ct = default)
    {
        var safetyEvent = await _db.Events.FirstOrDefaultAsync(e => e.Id == eventId, ct)
                          ?? throw NotFoundException.For(nameof(SafetyEvent), eventId);

        await _access.RequireMembershipAsync(safetyEvent.GroupId, userId, ct);

        safetyEvent.Acknowledged = true;
        safetyEvent.Touch();
        await _db.SaveChangesAsync(ct);

        return ToDto(safetyEvent);
    }

    private static EventDto ToDto(SafetyEvent e) => new(
        e.Id, e.GroupId, e.SubjectUserId, e.Type.ToString(), e.Severity.ToString(),
        e.Message, e.PlaceId, e.Latitude, e.Longitude, e.OccurredAt, e.Acknowledged);
}
