using AgriCheck.Application.Compliance;
using AgriCheck.Domain.Enums;
using AgriCheck.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AgriCheck.Infrastructure.Services.ComplianceSync;

public class ComplianceSyncProcessor : IComplianceSyncProcessor
{
    private const int MaxAttempts = 5;
    private readonly AgriCheckDbContext _db;
    private readonly IEnumerable<IComplianceSyncAdapter> _adapters;
    private readonly ILogger<ComplianceSyncProcessor> _logger;

    public ComplianceSyncProcessor(
        AgriCheckDbContext db,
        IEnumerable<IComplianceSyncAdapter> adapters,
        ILogger<ComplianceSyncProcessor> logger)
    {
        _db = db;
        _adapters = adapters;
        _logger = logger;
    }

    public async Task ProcessPendingAsync(CancellationToken cancellationToken = default)
    {
        var pending = await _db.ComplianceSyncOutbox
            .Where(x => x.Status == ComplianceSyncOutboxStatus.Pending && x.AttemptCount < MaxAttempts)
            .OrderBy(x => x.CreatedAt)
            .Take(20)
            .ToListAsync(cancellationToken);

        foreach (var row in pending)
        {
            var adapter = _adapters.FirstOrDefault(a => a.TargetSystem == row.TargetSystem);
            if (adapter is null)
            {
                row.AttemptCount++;
                row.LastError = $"No adapter for {row.TargetSystem}";
                if (row.AttemptCount >= MaxAttempts)
                {
                    row.Status = ComplianceSyncOutboxStatus.Failed;
                }

                continue;
            }

            row.AttemptCount++;
            try
            {
                var result = await adapter.DispatchAsync(row, cancellationToken);
                if (result.Success)
                {
                    row.Status = ComplianceSyncOutboxStatus.Succeeded;
                    row.SyncedAt = DateTime.UtcNow;
                    row.LastError = null;
                }
                else
                {
                    row.LastError = result.ErrorMessage ?? "Adapter returned failure";
                    if (row.AttemptCount >= MaxAttempts)
                    {
                        row.Status = ComplianceSyncOutboxStatus.Failed;
                    }
                }
            }
            catch (Exception ex)
            {
                row.LastError = ex.Message;
                _logger.LogWarning(ex, "Compliance sync failed for outbox {Id}", row.Id);
                if (row.AttemptCount >= MaxAttempts)
                {
                    row.Status = ComplianceSyncOutboxStatus.Failed;
                }
            }
        }

        if (pending.Count > 0)
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
    }
}
