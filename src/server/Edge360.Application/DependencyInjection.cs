using Edge360.Application.Admin;
using Edge360.Application.Auth;
using Edge360.Application.Common;
using Edge360.Application.Driving;
using Edge360.Application.Events;
using Edge360.Application.Geofencing;
using Edge360.Application.Groups;
using Edge360.Application.Locations;
using Edge360.Application.Notifications;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Edge360.Application;

/// <summary>Registers application-layer use-case services and validators.</summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<GroupAccess>();
        services.AddScoped<AuthService>();
        services.AddScoped<GroupService>();
        services.AddScoped<LocationService>();
        services.AddScoped<GeofenceService>();
        services.AddScoped<EventService>();

        // Driving intelligence. Thresholds come from IOptions when configured, else defaults.
        services.AddSingleton(sp => sp.GetService<IOptions<DrivingThresholds>>()?.Value ?? new DrivingThresholds());
        services.AddScoped<DrivingAnalyzer>();
        services.AddScoped<DrivingService>();

        // Notifications. Channels are registered by the infrastructure layer.
        services.AddSingleton(sp => sp.GetService<IOptions<NotificationOptions>>()?.Value ?? new NotificationOptions());
        services.AddScoped<INotificationDispatcher, NotificationDispatcher>();

        // Administration + retention.
        services.AddSingleton(sp => sp.GetService<IOptions<RetentionOptions>>()?.Value ?? new RetentionOptions());
        services.AddScoped<AdminService>();

        services.AddValidatorsFromAssemblyContaining<AuthService>(ServiceLifetime.Scoped);

        return services;
    }
}
