namespace AgriCheck.Application.Compliance;

public interface IComplianceSyncProcessor
{
    Task ProcessPendingAsync(CancellationToken cancellationToken = default);
}
