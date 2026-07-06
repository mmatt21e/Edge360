using Edge360.Application.Admin;
using Edge360.Application.Common.Exceptions;
using Edge360.Domain.Entities;
using Edge360.Domain.Enums;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Edge360.Application.Tests;

public class AdminServiceTests : IDisposable
{
    private readonly SqliteContextFactory _factory = new();

    private AdminService CreateSut(RetentionOptions? retention = null)
    {
        var ctx = _factory.Create();
        return new AdminService(ctx, new NullAuditWriter(), retention ?? new RetentionOptions());
    }

    private Guid SeedUser(string email, SystemRole role = SystemRole.User)
    {
        using var ctx = _factory.Create();
        var user = new User
        {
            Email = email,
            NormalizedEmail = email.ToUpperInvariant(),
            DisplayName = email.Split('@')[0],
            PasswordHash = "x",
            SystemRole = role
        };
        ctx.Users.Add(user);
        ctx.SaveChanges();
        return user.Id;
    }

    [Fact]
    public async Task ListUsers_ReturnsSeededUsers()
    {
        SeedUser("a@x.com");
        SeedUser("b@x.com");

        var page = await CreateSut().ListUsersAsync(1, 50, null);

        page.TotalCount.Should().Be(2);
    }

    [Fact]
    public async Task ListUsers_FiltersBySearch()
    {
        SeedUser("alice@x.com");
        SeedUser("bob@x.com");

        var page = await CreateSut().ListUsersAsync(1, 50, "alice");

        page.Items.Should().ContainSingle().Which.Email.Should().Be("alice@x.com");
    }

    [Fact]
    public async Task SetUserRole_PromotesToAdministrator()
    {
        var id = SeedUser("promote@x.com");

        var updated = await CreateSut().SetUserRoleAsync(Guid.NewGuid(), id, "Administrator");

        updated.SystemRole.Should().Be("Administrator");
        await using var verify = _factory.Create();
        (await verify.Users.FirstAsync(u => u.Id == id)).SystemRole.Should().Be(SystemRole.Administrator);
    }

    [Fact]
    public async Task SetUserRole_InvalidRole_Throws()
    {
        var id = SeedUser("bad@x.com");
        var act = async () => await CreateSut().SetUserRoleAsync(Guid.NewGuid(), id, "Wizard");
        await act.Should().ThrowAsync<AppValidationException>();
    }

    [Fact]
    public async Task SetUserActive_Deactivates()
    {
        var id = SeedUser("deact@x.com");
        var updated = await CreateSut().SetUserActiveAsync(Guid.NewGuid(), id, false);
        updated.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task GetStats_CountsUsers()
    {
        SeedUser("s1@x.com");
        SeedUser("s2@x.com", SystemRole.Administrator);

        var stats = await CreateSut().GetStatsAsync();

        stats.UserCount.Should().Be(2);
        stats.ActiveUserCount.Should().Be(2);
    }

    [Fact]
    public async Task RunRetention_PurgesOldLocationPointsOnly()
    {
        var userId = SeedUser("ret@x.com");
        using (var seed = _factory.Create())
        {
            seed.LocationPoints.Add(new LocationPoint { UserId = userId, Latitude = 1, Longitude = 1, RecordedAt = DateTimeOffset.UtcNow.AddDays(-100) });
            seed.LocationPoints.Add(new LocationPoint { UserId = userId, Latitude = 2, Longitude = 2, RecordedAt = DateTimeOffset.UtcNow.AddDays(-1) });
            seed.SaveChanges();
        }

        var result = await CreateSut(new RetentionOptions { LocationRetentionDays = 90 }).RunRetentionAsync(null);

        result.LocationPointsDeleted.Should().Be(1);
        await using var verify = _factory.Create();
        (await verify.LocationPoints.CountAsync()).Should().Be(1); // the recent one remains
    }

    public void Dispose() => _factory.Dispose();
}
