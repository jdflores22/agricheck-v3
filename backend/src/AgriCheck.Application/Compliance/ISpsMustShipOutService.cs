namespace AgriCheck.Application.Compliance;

public interface ISpsMustShipOutService
{
    DateTime ComputeMustShipOutBy(DateTime issuedAt, string? hsCode, string? commodityName);

    Task<int> GetValidityDaysAsync(string? hsCode, string? commodityName, CancellationToken cancellationToken = default);

    Task ExpireOverdueClearancesAsync(CancellationToken cancellationToken = default);
}
