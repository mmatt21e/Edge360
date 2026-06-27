using Edge360.Application.Common;
using Edge360.Application.Common.Abstractions;
using Edge360.Application.Common.Exceptions;
using Edge360.Application.Common.Models;
using Edge360.Application.Driving.Dtos;
using Edge360.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Edge360.Application.Driving;

/// <summary>Reconstructs and scores driving trips, and serves trip/score queries.</summary>
public sealed class DrivingService
{
    private readonly IAppDbContext _db;
    private readonly GroupAccess _access;
    private readonly DrivingAnalyzer _analyzer;
    private readonly IAuditWriter _audit;

    public DrivingService(IAppDbContext db, GroupAccess access, DrivingAnalyzer analyzer, IAuditWriter audit)
    {
        _db = db;
        _access = access;
        _analyzer = analyzer;
        _audit = audit;
    }

    /// <summary>Re-analyzes the caller's location history in the range into scored trips.</summary>
    public async Task<AnalyzeResultDto> AnalyzeAsync(Guid userId, AnalyzeDrivingRequest request, CancellationToken ct = default)
    {
        var from = request.From ?? DateTimeOffset.MinValue;
        var to = request.To ?? DateTimeOffset.MaxValue;

        var points = await _db.LocationPoints
            .Where(p => p.UserId == userId && p.RecordedAt >= from && p.RecordedAt <= to)
            .OrderBy(p => p.RecordedAt)
            .ToListAsync(ct);

        var trips = _analyzer.Analyze(userId, points);

        // Replace any previously-computed trips that start within the analyzed window.
        var stale = await _db.Trips
            .Where(t => t.UserId == userId && t.StartedAt >= from && t.StartedAt <= to)
            .ToListAsync(ct);
        if (stale.Count > 0)
            _db.Trips.RemoveRange(stale);

        foreach (var trip in trips)
            _db.Trips.Add(trip);

        await _db.SaveChangesAsync(ct);
        await _audit.WriteAsync("driving.analyze", userId, nameof(Trip), null, $"{trips.Count} trips", ct);

        return new AnalyzeResultDto(trips.Count, trips.Select(ToDto).ToList());
    }

    public async Task<PagedResult<TripDto>> ListTripsAsync(
        Guid callerId, Guid? subjectUserId, DateTimeOffset? from, DateTimeOffset? to,
        int page, int pageSize, CancellationToken ct = default)
    {
        var target = subjectUserId ?? callerId;
        await _access.RequireCanViewAsync(callerId, target, ct);

        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 200);

        var query = _db.Trips.Where(t => t.UserId == target);
        if (from is { } f) query = query.Where(t => t.StartedAt >= f);
        if (to is { } tt) query = query.Where(t => t.StartedAt <= tt);

        var total = await query.LongCountAsync(ct);
        var items = await query
            .OrderByDescending(t => t.StartedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(t => ToDto(t))
            .ToListAsync(ct);

        return new PagedResult<TripDto>(items, page, pageSize, total);
    }

    public async Task<TripDetailDto> GetTripAsync(Guid callerId, Guid tripId, CancellationToken ct = default)
    {
        var trip = await _db.Trips
            .Include(t => t.Events)
            .FirstOrDefaultAsync(t => t.Id == tripId, ct)
            ?? throw NotFoundException.For(nameof(Trip), tripId);

        await _access.RequireCanViewAsync(callerId, trip.UserId, ct);

        var route = await _db.LocationPoints
            .Where(p => p.UserId == trip.UserId && p.RecordedAt >= trip.StartedAt && p.RecordedAt <= trip.EndedAt)
            .OrderBy(p => p.RecordedAt)
            .Select(p => new[] { p.Latitude, p.Longitude })
            .ToListAsync(ct);

        var events = trip.Events
            .OrderBy(e => e.OccurredAt)
            .Select(e => new DrivingEventDto(e.Id, e.Type.ToString(), e.OccurredAt, e.Latitude, e.Longitude, e.Magnitude))
            .ToList();

        return new TripDetailDto(ToDto(trip), events, route);
    }

    public async Task<DrivingScoreDto> GetScoreSummaryAsync(
        Guid callerId, Guid? subjectUserId, DateTimeOffset? from, DateTimeOffset? to, CancellationToken ct = default)
    {
        var target = subjectUserId ?? callerId;
        await _access.RequireCanViewAsync(callerId, target, ct);

        var query = _db.Trips.Where(t => t.UserId == target);
        if (from is { } f) query = query.Where(t => t.StartedAt >= f);
        if (to is { } tt) query = query.Where(t => t.StartedAt <= tt);

        var trips = await query.ToListAsync(ct);
        if (trips.Count == 0)
            return new DrivingScoreDto(target, 0, 0, 100, 0, 0, 0, 0);

        return new DrivingScoreDto(
            target,
            trips.Count,
            trips.Sum(t => t.DistanceMeters),
            (int)Math.Round(trips.Average(t => t.Score)),
            trips.Sum(t => t.HardBrakingCount),
            trips.Sum(t => t.HardAccelerationCount),
            trips.Sum(t => t.HarshCorneringCount),
            trips.Sum(t => t.SpeedingCount));
    }

    private static TripDto ToDto(Trip t) => new(
        t.Id, t.UserId, t.StartedAt, t.EndedAt, t.DistanceMeters, t.DurationSeconds,
        t.MaxSpeedMps, t.AverageSpeedMps, t.HardBrakingCount, t.HardAccelerationCount,
        t.HarshCorneringCount, t.SpeedingCount, t.Score);
}
