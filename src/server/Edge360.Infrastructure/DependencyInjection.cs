using Edge360.Application.Common.Abstractions;
using Edge360.Application.Notifications;
using Edge360.Infrastructure.Auditing;
using Edge360.Infrastructure.Identity;
using Edge360.Infrastructure.Notifications;
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

        // Notification options + channels (the dispatcher consumes all enabled channels).
        services.Configure<NotificationOptions>(configuration.GetSection(NotificationOptions.SectionName));
        services.Configure<EmailOptions>(configuration.GetSection(EmailOptions.SectionName));
        services.Configure<WebhookOptions>(configuration.GetSection(WebhookOptions.SectionName));
        services.Configure<LoggingChannelOptions>(configuration.GetSection(LoggingChannelOptions.SectionName));

        services.AddSingleton<INotificationChannel, LoggingNotificationChannel>();
        services.AddSingleton<INotificationChannel, SmtpEmailNotificationChannel>();
        services.AddHttpClient<WebhookNotificationChannel>();
        services.AddTransient<INotificationChannel>(sp => sp.GetRequiredService<WebhookNotificationChannel>());

        return services;
    }
}
