using AgriCheck.Application.Compliance;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AgriCheck.Infrastructure.Services;

/// <summary>
/// Stub worker for RSAS / DA Trade sync adapters (P8.8). Processes pending outbox rows when adapters exist.
/// </summary>
public class ComplianceSyncOutboxWorker : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromMinutes(2);
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ComplianceSyncOutboxWorker> _logger;

    public ComplianceSyncOutboxWorker(IServiceScopeFactory scopeFactory, ILogger<ComplianceSyncOutboxWorker> logger)
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
                await ProcessBatchAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogWarning(ex, "Compliance sync outbox batch failed.");
            }

            await Task.Delay(PollInterval, stoppingToken);
        }
    }

    private async Task ProcessBatchAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var processor = scope.ServiceProvider.GetRequiredService<IComplianceSyncProcessor>();
        await processor.ProcessPendingAsync(cancellationToken);
    }
}
