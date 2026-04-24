using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ProfileService.Domain.Interfaces.Services.ErrorCodeResolver;

namespace ProfileService.Infrastructure.Services.BackgroundServices;

/// <summary>
/// Periodically refreshes the error code in-memory and Redis caches
/// by fetching all codes from UtilityService every 24 hours.
/// </summary>
public class ErrorCodeCacheRefreshService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ErrorCodeCacheRefreshService> _logger;
    private static readonly TimeSpan RefreshInterval = TimeSpan.FromHours(24);

    public ErrorCodeCacheRefreshService(
        IServiceScopeFactory scopeFactory,
        ILogger<ErrorCodeCacheRefreshService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var resolver = scope.ServiceProvider
                    .GetRequiredService<IErrorCodeResolverService>();
                await resolver.RefreshCacheAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "Error code cache refresh failed. Will retry in {Interval}.",
                    RefreshInterval);
            }

            await Task.Delay(RefreshInterval, stoppingToken);
        }
    }
}
