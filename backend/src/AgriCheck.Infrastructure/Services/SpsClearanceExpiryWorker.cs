using AgriCheck.Application.Compliance;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AgriCheck.Infrastructure.Services;

public class SpsClearanceExpiryWorker : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromHours(1);
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SpsClearanceExpiryWorker> _logger;

    public SpsClearanceExpiryWorker(IServiceScopeFactory scopeFactory, ILogger<SpsClearanceExpiryWorker> logger)
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
                var service = scope.ServiceProvider.GetRequiredService<ISpsMustShipOutService>();
                await service.ExpireOverdueClearancesAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogWarning(ex, "SPS clearance expiry pass failed.");
            }

            await Task.Delay(PollInterval, stoppingToken);
        }
    }
}
