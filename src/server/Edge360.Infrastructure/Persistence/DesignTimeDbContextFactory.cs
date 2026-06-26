using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Edge360.Infrastructure.Persistence;

/// <summary>
/// Used by the EF Core tools (migrations) at design time so they never execute the API's
/// runtime startup. The connection string here is only for scaffolding, not runtime.
/// </summary>
public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("EDGE360_DESIGN_CONNECTION")
            ?? "Host=localhost;Port=5432;Database=edge360;Username=edge360;Password=edge360";

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new AppDbContext(options);
    }
}
