using Edge360.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Edge360.Infrastructure.Persistence.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> b)
    {
        b.ToTable("users");
        b.HasKey(x => x.Id);
        b.Property(x => x.Email).IsRequired().HasMaxLength(256);
        b.Property(x => x.NormalizedEmail).IsRequired().HasMaxLength(256);
        b.Property(x => x.DisplayName).IsRequired().HasMaxLength(100);
        b.Property(x => x.PasswordHash).IsRequired();
        b.HasIndex(x => x.NormalizedEmail).IsUnique();

        b.HasMany(x => x.Devices).WithOne(d => d.User).HasForeignKey(d => d.UserId).OnDelete(DeleteBehavior.Cascade);
        b.HasMany(x => x.Memberships).WithOne(m => m.User).HasForeignKey(m => m.UserId).OnDelete(DeleteBehavior.Cascade);
        b.HasMany(x => x.RefreshTokens).WithOne(t => t.User).HasForeignKey(t => t.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> b)
    {
        b.ToTable("refresh_tokens");
        b.HasKey(x => x.Id);
        b.Property(x => x.TokenHash).IsRequired().HasMaxLength(128);
        b.HasIndex(x => x.TokenHash).IsUnique();
        b.Ignore(x => x.IsActive);
    }
}

public sealed class DeviceConfiguration : IEntityTypeConfiguration<Device>
{
    public void Configure(EntityTypeBuilder<Device> b)
    {
        b.ToTable("devices");
        b.HasKey(x => x.Id);
        b.Property(x => x.Name).IsRequired().HasMaxLength(100);
        b.HasIndex(x => x.UserId);
        b.HasMany(x => x.LocationPoints).WithOne(p => p.Device).HasForeignKey(p => p.DeviceId).OnDelete(DeleteBehavior.SetNull);
    }
}

public sealed class GroupConfiguration : IEntityTypeConfiguration<Group>
{
    public void Configure(EntityTypeBuilder<Group> b)
    {
        b.ToTable("groups");
        b.HasKey(x => x.Id);
        b.Property(x => x.Name).IsRequired().HasMaxLength(100);
        b.Property(x => x.InviteCode).IsRequired().HasMaxLength(16);
        b.HasIndex(x => x.InviteCode).IsUnique();

        b.HasMany(x => x.Memberships).WithOne(m => m.Group).HasForeignKey(m => m.GroupId).OnDelete(DeleteBehavior.Cascade);
        b.HasMany(x => x.Places).WithOne(p => p.Group).HasForeignKey(p => p.GroupId).OnDelete(DeleteBehavior.Cascade);
        b.HasMany(x => x.Events).WithOne(e => e.Group).HasForeignKey(e => e.GroupId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class GroupMembershipConfiguration : IEntityTypeConfiguration<GroupMembership>
{
    public void Configure(EntityTypeBuilder<GroupMembership> b)
    {
        b.ToTable("group_memberships");
        b.HasKey(x => x.Id);
        b.HasIndex(x => new { x.GroupId, x.UserId }).IsUnique();
        b.Ignore(x => x.IsSharingActive);
    }
}

public sealed class LocationPointConfiguration : IEntityTypeConfiguration<LocationPoint>
{
    public void Configure(EntityTypeBuilder<LocationPoint> b)
    {
        b.ToTable("location_points");
        b.HasKey(x => x.Id);
        // Primary access pattern: latest/history per member, time-ordered.
        b.HasIndex(x => new { x.UserId, x.RecordedAt });
        b.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class PlaceConfiguration : IEntityTypeConfiguration<Place>
{
    public void Configure(EntityTypeBuilder<Place> b)
    {
        b.ToTable("places");
        b.HasKey(x => x.Id);
        b.Property(x => x.Name).IsRequired().HasMaxLength(100);
        b.HasIndex(x => x.GroupId);
        b.Ignore(x => x.Center);
    }
}

public sealed class SafetyEventConfiguration : IEntityTypeConfiguration<SafetyEvent>
{
    public void Configure(EntityTypeBuilder<SafetyEvent> b)
    {
        b.ToTable("safety_events");
        b.HasKey(x => x.Id);
        b.Property(x => x.Message).IsRequired().HasMaxLength(1000);
        b.HasIndex(x => new { x.GroupId, x.OccurredAt });
        b.HasOne(x => x.Place).WithMany().HasForeignKey(x => x.PlaceId).OnDelete(DeleteBehavior.SetNull);
        b.HasMany(x => x.Alerts).WithOne(a => a.Event).HasForeignKey(a => a.EventId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class AlertConfiguration : IEntityTypeConfiguration<Alert>
{
    public void Configure(EntityTypeBuilder<Alert> b)
    {
        b.ToTable("alerts");
        b.HasKey(x => x.Id);
        b.Property(x => x.Channel).IsRequired().HasMaxLength(32);
        b.HasIndex(x => x.RecipientUserId);
    }
}

public sealed class TripConfiguration : IEntityTypeConfiguration<Trip>
{
    public void Configure(EntityTypeBuilder<Trip> b)
    {
        b.ToTable("trips");
        b.HasKey(x => x.Id);
        b.HasIndex(x => new { x.UserId, x.StartedAt });
        b.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        b.HasMany(x => x.Events).WithOne(e => e.Trip).HasForeignKey(e => e.TripId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class DrivingEventConfiguration : IEntityTypeConfiguration<DrivingEvent>
{
    public void Configure(EntityTypeBuilder<DrivingEvent> b)
    {
        b.ToTable("driving_events");
        b.HasKey(x => x.Id);
        b.HasIndex(x => new { x.UserId, x.OccurredAt });
        b.HasIndex(x => x.TripId);
    }
}

public sealed class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> b)
    {
        b.ToTable("audit_logs");
        b.HasKey(x => x.Id);
        b.Property(x => x.Action).IsRequired().HasMaxLength(128);
        b.HasIndex(x => new { x.ActorUserId, x.CreatedAt });
    }
}
