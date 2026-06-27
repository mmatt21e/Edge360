using Edge360.Application.Common.Abstractions;
using Edge360.Application.Events.Dtos;
using Edge360.Application.Locations.Dtos;
using Edge360.Domain.Entities;
using Edge360.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Edge360.Application.Tests;

/// <summary>
/// Creates a real <see cref="AppDbContext"/> backed by a private SQLite in-memory database.
/// The connection is kept open for the lifetime of the context so the schema persists.
/// </summary>
public sealed class SqliteContextFactory : IDisposable
{
    private readonly SqliteConnection _connection;

    public SqliteContextFactory()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;

        using var ctx = new AppDbContext(options);
        ctx.Database.EnsureCreated();
        _options = options;
    }

    private readonly DbContextOptions<AppDbContext> _options;

    public AppDbContext Create() => new(_options);

    public void Dispose() => _connection.Dispose();
}

/// <summary>No-op token service producing deterministic values for tests.</summary>
public sealed class FakeTokenService : ITokenService
{
    public (string token, DateTimeOffset expiresAt) CreateAccessToken(User user) =>
        ($"access-{user.Id}", DateTimeOffset.UtcNow.AddMinutes(15));

    public (string raw, string hash) CreateRefreshToken()
    {
        var raw = Guid.NewGuid().ToString("N");
        return (raw, HashRefreshToken(raw));
    }

    public string HashRefreshToken(string raw) => $"hash:{raw}";
}

/// <summary>Records calls so tests can assert real-time fan-out happened.</summary>
public sealed class RecordingNotifier : IRealtimeNotifier
{
    public List<(Guid GroupId, LocationDto Location)> Locations { get; } = new();
    public List<(Guid GroupId, EventDto Event)> Events { get; } = new();

    public Task LocationUpdatedAsync(Guid groupId, LocationDto location, CancellationToken ct = default)
    {
        Locations.Add((groupId, location));
        return Task.CompletedTask;
    }

    public Task EventRaisedAsync(Guid groupId, EventDto safetyEvent, CancellationToken ct = default)
    {
        Events.Add((groupId, safetyEvent));
        return Task.CompletedTask;
    }
}

/// <summary>No-op audit writer.</summary>
public sealed class NullAuditWriter : IAuditWriter
{
    public Task WriteAsync(string action, Guid? actorUserId = null, string? targetType = null,
        string? targetId = null, string? metadata = null, CancellationToken ct = default) => Task.CompletedTask;
}

/// <summary>No-op notification dispatcher that records which event ids were dispatched.</summary>
public sealed class RecordingDispatcher : Edge360.Application.Notifications.INotificationDispatcher
{
    public List<Guid> Dispatched { get; } = new();

    public Task DispatchEventAsync(Guid eventId, CancellationToken ct = default)
    {
        Dispatched.Add(eventId);
        return Task.CompletedTask;
    }
}

/// <summary>Configurable in-memory notification channel for dispatcher tests.</summary>
public sealed class FakeChannel : Edge360.Application.Notifications.INotificationChannel
{
    private readonly Func<Edge360.Application.Notifications.NotificationMessage, Edge360.Application.Notifications.NotificationResult> _behavior;

    public FakeChannel(string name, bool enabled,
        Func<Edge360.Application.Notifications.NotificationMessage, Edge360.Application.Notifications.NotificationResult>? behavior = null)
    {
        Name = name;
        IsEnabled = enabled;
        _behavior = behavior ?? (_ => Edge360.Application.Notifications.NotificationResult.Sent());
    }

    public string Name { get; }
    public bool IsEnabled { get; }
    public List<Edge360.Application.Notifications.NotificationMessage> Sent { get; } = new();

    public Task<Edge360.Application.Notifications.NotificationResult> SendAsync(
        Edge360.Application.Notifications.NotificationMessage message, CancellationToken ct = default)
    {
        Sent.Add(message);
        return Task.FromResult(_behavior(message));
    }
}
