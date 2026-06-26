using Edge360.Application.Common.Abstractions;
using Edge360.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Edge360.Infrastructure.Persistence;

/// <summary>EF Core context and the concrete implementation of <see cref="IAppDbContext"/>.</summary>
public sealed class AppDbContext : DbContext, IAppDbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Device> Devices => Set<Device>();
    public DbSet<Group> Groups => Set<Group>();
    public DbSet<GroupMembership> Memberships => Set<GroupMembership>();
    public DbSet<LocationPoint> LocationPoints => Set<LocationPoint>();
    public DbSet<Place> Places => Set<Place>();
    public DbSet<SafetyEvent> Events => Set<SafetyEvent>();
    public DbSet<Alert> Alerts => Set<Alert>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        base.ConfigureConventions(configurationBuilder);

        // SQLite (used by the test suite) cannot ORDER BY DateTimeOffset. Store it as an
        // order-preserving binary value there; Npgsql keeps its native timestamptz mapping.
        if (Database.ProviderName == "Microsoft.EntityFrameworkCore.Sqlite")
        {
            configurationBuilder.Properties<DateTimeOffset>()
                .HaveConversion<DateTimeOffsetToBinaryConverter>();
        }
    }
}
