using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Orders.Application.Recovery;

namespace Orders.Infrastructure.Recovery;

public sealed class CompensationRecoveryWorker(
    IServiceScopeFactory scopeFactory,
    ILogger<CompensationRecoveryWorker> logger) : BackgroundService
{
    private const int MaxAttempts = 10;
    private const int BatchSize = 20;
    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(15);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var service = scope.ServiceProvider.GetRequiredService<CompensationRecoveryService>();
                var processed = await service.RunOnceAsync(MaxAttempts, BatchSize, stoppingToken);

                if (processed > 0)
                {
                    logger.LogInformation("Recovery cycle: {Count} orders process hue.", processed);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Recovery cycle fail. Agli cycle mein dobara.");
            }

            await Task.Delay(Interval, stoppingToken);
        }
    }
}
