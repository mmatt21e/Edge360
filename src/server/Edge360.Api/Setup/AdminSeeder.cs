using Edge360.Application.Common.Abstractions;
using Edge360.Domain.Entities;
using Edge360.Domain.Enums;
using Edge360.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Edge360.Api.Setup;

/// <summary>
/// Creates an initial administrator account from configuration ("AdminSeed") when one does not
/// already exist. Lets a fresh deployment bootstrap an admin without manual DB edits.
/// </summary>
public static class AdminSeeder
{
    public static async Task SeedAsync(IServiceProvider services, IConfiguration configuration)
    {
        var email = configuration["AdminSeed:Email"];
        var password = configuration["AdminSeed:Password"];
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            return;

        var db = services.GetRequiredService<AppDbContext>();
        var hasher = services.GetRequiredService<IPasswordHasher>();

        var normalized = email.Trim().ToUpperInvariant();
        if (await db.Users.AnyAsync(u => u.NormalizedEmail == normalized))
            return;

        db.Users.Add(new User
        {
            Email = email.Trim(),
            NormalizedEmail = normalized,
            DisplayName = configuration["AdminSeed:DisplayName"] ?? "Administrator",
            PasswordHash = hasher.Hash(password),
            SystemRole = SystemRole.Administrator
        });
        await db.SaveChangesAsync();
    }
}
