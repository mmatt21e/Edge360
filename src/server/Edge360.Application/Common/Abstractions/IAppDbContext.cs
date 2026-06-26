using Edge360.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Edge360.Application.Common.Abstractions;

/// <summary>
/// Persistence boundary used by the application layer. Implemented by the EF Core DbContext
/// in the infrastructure layer so use cases stay free of a concrete provider.
/// </summary>
public interface IAppDbContext
{
    DbSet<User> Users { get; }
    DbSet<RefreshToken> RefreshTokens { get; }
    DbSet<Device> Devices { get; }
    DbSet<Group> Groups { get; }
    DbSet<GroupMembership> Memberships { get; }
    DbSet<LocationPoint> LocationPoints { get; }
    DbSet<Place> Places { get; }
    DbSet<SafetyEvent> Events { get; }
    DbSet<Alert> Alerts { get; }
    DbSet<AuditLog> AuditLogs { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
