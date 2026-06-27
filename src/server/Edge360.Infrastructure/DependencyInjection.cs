using Edge360.Application.Common.Abstractions;
using Edge360.Infrastructure.Auditing;
using Edge360.Infrastructure.Identity;
using Edge360.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Edge360.Infrastructure;

/// <summary>Registers infrastructure services: persistence, identity, auditing.</summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default")
            ?? "Host=localhost;Database=edge360;Username=edge360;Password=edge360";

        services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));
        services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<AppDbContext>());

        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.Configure<Application.Driving.DrivingThresholds>(
            configuration.GetSection(Application.Driving.DrivingThresholds.SectionName));

        services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();
        services.AddSingleton<ITokenService, JwtTokenService>();
        services.AddScoped<IAuditWriter, AuditWriter>();

        return services;
    }
}
