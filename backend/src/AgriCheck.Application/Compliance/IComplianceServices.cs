namespace AgriCheck.Application.Compliance;

public interface IComplianceGateService
{
    Task<ComplianceEvaluationResult> EvaluateAsync(ComplianceGateContext context, CancellationToken cancellationToken = default);
    Task RequireAsync(ComplianceGateContext context, CancellationToken cancellationToken = default);
}

public interface IComplianceChainService
{
    Task<EntryComplianceChainDto?> GetForEntryAsync(Guid entryUuid, CancellationToken cancellationToken = default);

    Task<EntryComplianceChainDto?> GetForEntryAsAgencyAsync(Guid entryUuid, CancellationToken cancellationToken = default);
}
