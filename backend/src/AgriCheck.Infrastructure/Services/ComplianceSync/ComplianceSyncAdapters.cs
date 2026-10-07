using AgriCheck.Domain.Entities;
using AgriCheck.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace AgriCheck.Infrastructure.Services.ComplianceSync;

public interface IComplianceSyncAdapter
{
    ComplianceSyncTargetSystem TargetSystem { get; }

    Task<ComplianceSyncAdapterResult> DispatchAsync(ComplianceSyncOutbox row, CancellationToken cancellationToken);
}

public record ComplianceSyncAdapterResult(bool Success, string? ExternalReference, string? ErrorMessage);

public sealed class RsasSyncAdapter : IComplianceSyncAdapter
{
    private readonly ILogger<RsasSyncAdapter> _logger;

    public RsasSyncAdapter(ILogger<RsasSyncAdapter> logger) => _logger = logger;

    public ComplianceSyncTargetSystem TargetSystem => ComplianceSyncTargetSystem.Rsas;

    public Task<ComplianceSyncAdapterResult> DispatchAsync(ComplianceSyncOutbox row, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "RSAS adapter stub: {Operation} entity {EntityType}#{EntityId} payload {Payload}",
            row.Operation,
            row.EntityType,
            row.EntityId,
            row.PayloadJson);

        return Task.FromResult(new ComplianceSyncAdapterResult(true, $"RSAS-STUB-{row.EntityId}", null));
    }
}

public sealed class DaTradeSpsicAdapter : IComplianceSyncAdapter
{
    private readonly ILogger<DaTradeSpsicAdapter> _logger;

    public DaTradeSpsicAdapter(ILogger<DaTradeSpsicAdapter> logger) => _logger = logger;

    public ComplianceSyncTargetSystem TargetSystem => ComplianceSyncTargetSystem.DaTradeSpsic;

    public Task<ComplianceSyncAdapterResult> DispatchAsync(ComplianceSyncOutbox row, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "DA Trade SPSIC adapter stub: {Operation} entity {EntityType}#{EntityId}",
            row.Operation,
            row.EntityType,
            row.EntityId);

        return Task.FromResult(new ComplianceSyncAdapterResult(true, $"DATRADE-SPSIC-{row.EntityId}", null));
    }
}

public sealed class DaTradeErfiAdapter : IComplianceSyncAdapter
{
    private readonly ILogger<DaTradeErfiAdapter> _logger;

    public DaTradeErfiAdapter(ILogger<DaTradeErfiAdapter> logger) => _logger = logger;

    public ComplianceSyncTargetSystem TargetSystem => ComplianceSyncTargetSystem.DaTradeErfi;

    public Task<ComplianceSyncAdapterResult> DispatchAsync(ComplianceSyncOutbox row, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "DA Trade e-RFI adapter stub: {Operation} entity {EntityType}#{EntityId}",
            row.Operation,
            row.EntityType,
            row.EntityId);

        return Task.FromResult(new ComplianceSyncAdapterResult(true, $"DATRADE-ERFI-{row.EntityId}", null));
    }
}

public sealed class NationalSingleWindowAdapter : IComplianceSyncAdapter
{
    private readonly ILogger<NationalSingleWindowAdapter> _logger;

    public NationalSingleWindowAdapter(ILogger<NationalSingleWindowAdapter> logger) => _logger = logger;

    public ComplianceSyncTargetSystem TargetSystem => ComplianceSyncTargetSystem.NationalSingleWindow;

    public Task<ComplianceSyncAdapterResult> DispatchAsync(ComplianceSyncOutbox row, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "NSW/IFM adapter stub: {Operation} entity {EntityType}#{EntityId}",
            row.Operation,
            row.EntityType,
            row.EntityId);

        return Task.FromResult(new ComplianceSyncAdapterResult(true, $"NSW-STUB-{row.EntityId}", null));
    }
}
