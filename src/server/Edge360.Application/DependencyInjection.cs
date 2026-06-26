using Edge360.Application.Auth;
using Edge360.Application.Common;
using Edge360.Application.Events;
using Edge360.Application.Geofencing;
using Edge360.Application.Groups;
using Edge360.Application.Locations;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

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

        services.AddValidatorsFromAssemblyContaining<AuthService>(ServiceLifetime.Scoped);

        return services;
    }
}
