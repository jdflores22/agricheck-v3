using AgriCheck.Domain.Enums;
using AgriCheck.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AgriCheck.Infrastructure.Services;

public class FacilityRegistrationExpiryWorker : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromHours(12);
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<FacilityRegistrationExpiryWorker> _logger;

    public FacilityRegistrationExpiryWorker(IServiceScopeFactory scopeFactory, ILogger<FacilityRegistrationExpiryWorker> logger)
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
                await ExpireOverdueAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogWarning(ex, "Facility registration expiry sweep failed.");
            }

            await Task.Delay(PollInterval, stoppingToken);
        }
    }

    private async Task ExpireOverdueAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AgriCheckDbContext>();
        var now = DateTime.UtcNow;

        var overdue = await db.FacilityRegistrations
            .Where(r => r.Status == FacilityRegistrationStatus.Registered
                && r.ExpiresAt != null
                && r.ExpiresAt < now)
            .ToListAsync(cancellationToken);

        if (overdue.Count == 0)
        {
            return;
        }

        foreach (var registration in overdue)
        {
            registration.Status = FacilityRegistrationStatus.Expired;
        }

        await db.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Marked {Count} RSAS registration(s) as expired.", overdue.Count);
    }
}
