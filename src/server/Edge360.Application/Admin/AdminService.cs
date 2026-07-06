using Edge360.Application.Admin.Dtos;
using Edge360.Application.Common.Abstractions;
using Edge360.Application.Common.Exceptions;
using Edge360.Application.Common.Models;
using Edge360.Domain.Entities;
using Edge360.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Edge360.Application.Admin;

/// <summary>
/// Platform-administration use cases: user management, group oversight, audit browsing, system
/// stats and data retention. Authorization (Administrator role) is enforced at the API boundary.
/// </summary>
public sealed class AdminService
{
    private readonly IAppDbContext _db;
    private readonly IAuditWriter _audit;
    private readonly RetentionOptions _retention;

    public AdminService(IAppDbContext db, IAuditWriter audit, RetentionOptions retention)
    {
        _db = db;
        _audit = audit;
        _retention = retention;
    }

    public async Task<PagedResult<AdminUserDto>> ListUsersAsync(int page, int pageSize, string? search, CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 200);

        var query = _db.Users.AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToUpperInvariant();
            query = query.Where(u => u.NormalizedEmail.Contains(term) || u.DisplayName.ToUpper().Contains(term));
        }

        var total = await query.LongCountAsync(ct);
        var items = await query
            .OrderBy(u => u.Email)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(u => new AdminUserDto(
                u.Id, u.Email, u.DisplayName, u.SystemRole.ToString(), u.IsActive,
                u.Memberships.Count, u.Devices.Count, u.LastLoginAt, u.CreatedAt))
            .ToListAsync(ct);

        return new PagedResult<AdminUserDto>(items, page, pageSize, total);
    }

    public async Task<AdminUserDto> SetUserRoleAsync(Guid adminId, Guid userId, string role, CancellationToken ct = default)
    {
        if (!Enum.TryParse<SystemRole>(role, ignoreCase: true, out var parsed))
            throw new AppValidationException(new Dictionary<string, string[]>
            {
                ["systemRole"] = new[] { "Must be 'User' or 'Administrator'." }
            });

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct)
                   ?? throw NotFoundException.For(nameof(User), userId);

        user.SystemRole = parsed;
        user.Touch();
        await _db.SaveChangesAsync(ct);
        await _audit.WriteAsync("admin.user.setRole", adminId, nameof(User), userId.ToString(), parsed.ToString(), ct);

        return await ProjectUserAsync(userId, ct);
    }

    public async Task<AdminUserDto> SetUserActiveAsync(Guid adminId, Guid userId, bool isActive, CancellationToken ct = default)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct)
                   ?? throw NotFoundException.For(nameof(User), userId);

        user.IsActive = isActive;
        user.Touch();
        await _db.SaveChangesAsync(ct);
        await _audit.WriteAsync("admin.user.setActive", adminId, nameof(User), userId.ToString(), isActive.ToString(), ct);

        return await ProjectUserAsync(userId, ct);
    }

    public async Task<PagedResult<AdminGroupDto>> ListGroupsAsync(int page, int pageSize, CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 200);

        var total = await _db.Groups.LongCountAsync(ct);
        var items = await _db.Groups
            .OrderBy(g => g.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(g => new AdminGroupDto(
                g.Id, g.Name, g.InviteCode, g.Memberships.Count, g.Places.Count, g.CreatedByUserId, g.CreatedAt))
            .ToListAsync(ct);

        return new PagedResult<AdminGroupDto>(items, page, pageSize, total);
    }

    public async Task<PagedResult<AuditLogDto>> ListAuditAsync(int page, int pageSize, string? action, CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 200);

        var query = _db.AuditLogs.AsQueryable();
        if (!string.IsNullOrWhiteSpace(action))
            query = query.Where(a => a.Action.Contains(action.Trim()));

        var total = await query.LongCountAsync(ct);
        var items = await query
            .OrderByDescending(a => a.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new AuditLogDto(a.Id, a.ActorUserId, a.Action, a.TargetType, a.TargetId, a.IpAddress, a.CreatedAt))
            .ToListAsync(ct);

        return new PagedResult<AuditLogDto>(items, page, pageSize, total);
    }

    public async Task<SystemStatsDto> GetStatsAsync(CancellationToken ct = default)
    {
        var since = DateTimeOffset.UtcNow.AddHours(-24);
        return new SystemStatsDto(
            UserCount: await _db.Users.CountAsync(ct),
            ActiveUserCount: await _db.Users.CountAsync(u => u.IsActive, ct),
            GroupCount: await _db.Groups.CountAsync(ct),
            DeviceCount: await _db.Devices.CountAsync(ct),
            LocationPointCount: await _db.LocationPoints.LongCountAsync(ct),
            EventsLast24h: await _db.Events.LongCountAsync(e => e.OccurredAt >= since, ct),
            TripCount: await _db.Trips.LongCountAsync(ct));
    }

    public RetentionPolicyDto GetRetentionPolicy() => new(
        _retention.LocationRetentionDays, _retention.EventRetentionDays, _retention.AuditRetentionDays, _retention.Enabled);

    /// <summary>Purges data older than the retention windows. Returns the number of rows removed.</summary>
    public async Task<RetentionResultDto> RunRetentionAsync(Guid? adminId, CancellationToken ct = default)
    {
        var now = DateTimeOffset.UtcNow;

        var locDeleted = await _db.LocationPoints
            .Where(p => p.RecordedAt < now.AddDays(-_retention.LocationRetentionDays))
            .ExecuteDeleteAsync(ct);

        var evtDeleted = await _db.Events
            .Where(e => e.OccurredAt < now.AddDays(-_retention.EventRetentionDays))
            .ExecuteDeleteAsync(ct);

        var auditDeleted = await _db.AuditLogs
            .Where(a => a.CreatedAt < now.AddDays(-_retention.AuditRetentionDays))
            .ExecuteDeleteAsync(ct);

        await _audit.WriteAsync("admin.retention.run", adminId, metadata: $"loc={locDeleted},evt={evtDeleted},audit={auditDeleted}", ct: ct);
        return new RetentionResultDto(locDeleted, evtDeleted, auditDeleted);
    }

    private async Task<AdminUserDto> ProjectUserAsync(Guid userId, CancellationToken ct) =>
        await _db.Users
            .Where(u => u.Id == userId)
            .Select(u => new AdminUserDto(
                u.Id, u.Email, u.DisplayName, u.SystemRole.ToString(), u.IsActive,
                u.Memberships.Count, u.Devices.Count, u.LastLoginAt, u.CreatedAt))
            .FirstAsync(ct);
}
