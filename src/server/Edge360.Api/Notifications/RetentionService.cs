using Edge360.Application.Admin;

namespace Edge360.Api.Notifications;

/// <summary>Periodically runs the data-retention purge when enabled in configuration.</summary>
public sealed class RetentionService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly RetentionOptions _options;
    private readonly ILogger<RetentionService> _logger;

    public RetentionService(IServiceScopeFactory scopeFactory, RetentionOptions options, ILogger<RetentionService> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = TimeSpan.FromHours(Math.Max(1, _options.IntervalHours));
        using var timer = new PeriodicTimer(interval);

        while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var admin = scope.ServiceProvider.GetRequiredService<AdminService>();
                var result = await admin.RunRetentionAsync(null, stoppingToken);
                _logger.LogInformation(
                    "Retention purge: {Loc} locations, {Evt} events, {Audit} audit logs removed.",
                    result.LocationPointsDeleted, result.EventsDeleted, result.AuditLogsDeleted);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Retention purge failed");
            }
        }
    }
}
