using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Edge360.Domain.Enums;
using Edge360.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Edge360.Api.IntegrationTests;

public class AdminEndToEndTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public AdminEndToEndTests(CustomWebApplicationFactory factory) => _factory = factory;

    private sealed record UserDto(Guid Id, string Email, string DisplayName, string SystemRole);
    private sealed record AuthResponse(string AccessToken, DateTimeOffset AccessTokenExpiresAt, string RefreshToken, UserDto User);
    private sealed record AdminUserDto(Guid Id, string Email, string DisplayName, string SystemRole, bool IsActive, int GroupCount, int DeviceCount, DateTimeOffset? LastLoginAt, DateTimeOffset CreatedAt);
    private sealed record PagedUsers(List<AdminUserDto> Items, int Page, int PageSize, long TotalCount);
    private sealed record StatsDto(int UserCount, int ActiveUserCount, int GroupCount, int DeviceCount, long LocationPointCount, long EventsLast24h, long TripCount);

    private async Task<(HttpClient client, AuthResponse auth)> RegisterAsync(string email)
    {
        var client = _factory.CreateClient();
        var resp = await client.PostAsJsonAsync("/api/auth/register",
            new { email, password = "password123", displayName = email.Split('@')[0] });
        var auth = (await resp.Content.ReadFromJsonAsync<AuthResponse>())!;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);
        return (client, auth);
    }

    private async Task PromoteAsync(Guid userId)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var user = await db.Users.FirstAsync(u => u.Id == userId);
        user.SystemRole = SystemRole.Administrator;
        await db.SaveChangesAsync();
    }

    private async Task<HttpClient> LoginAsync(string email)
    {
        var client = _factory.CreateClient();
        var resp = await client.PostAsJsonAsync("/api/auth/login", new { email, password = "password123" });
        var auth = (await resp.Content.ReadFromJsonAsync<AuthResponse>())!;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);
        return client;
    }

    [Fact]
    public async Task NonAdmin_IsForbidden_FromAdminEndpoints()
    {
        var (client, _) = await RegisterAsync($"plain-{Guid.NewGuid():N}@x.com");
        (await client.GetAsync("/api/admin/users")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await client.GetAsync("/api/admin/stats")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Admin_CanListUsersAndReadStats()
    {
        var email = $"admin-{Guid.NewGuid():N}@x.com";
        var (_, auth) = await RegisterAsync(email);
        await PromoteAsync(auth.User.Id);

        // A new token is needed so the JWT carries the Administrator role.
        var admin = await LoginAsync(email);

        var users = await admin.GetFromJsonAsync<PagedUsers>("/api/admin/users");
        users!.Items.Should().Contain(u => u.Email == email && u.SystemRole == "Administrator");

        var stats = await admin.GetFromJsonAsync<StatsDto>("/api/admin/stats");
        stats!.UserCount.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Admin_CanPromoteAnotherUser()
    {
        var adminEmail = $"root-{Guid.NewGuid():N}@x.com";
        var (_, adminAuth) = await RegisterAsync(adminEmail);
        await PromoteAsync(adminAuth.User.Id);
        var admin = await LoginAsync(adminEmail);

        var (_, targetAuth) = await RegisterAsync($"target-{Guid.NewGuid():N}@x.com");

        var resp = await admin.PostAsJsonAsync($"/api/admin/users/{targetAuth.User.Id}/role",
            new { systemRole = "Administrator" });
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        (await resp.Content.ReadFromJsonAsync<AdminUserDto>())!.SystemRole.Should().Be("Administrator");
    }
}
