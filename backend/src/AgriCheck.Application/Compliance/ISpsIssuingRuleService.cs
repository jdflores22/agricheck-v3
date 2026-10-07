namespace AgriCheck.Application.Compliance;

public interface ISpsIssuingRuleService
{
    Task<SpsIssuingAgencyMatchDto?> ResolveIssuerAsync(string? hsCode, long? fallbackAgencyId, CancellationToken cancellationToken = default);
}

public record SpsIssuingAgencyMatchDto(
    long AgencyId,
    string AgencyCode,
    string AgencyName,
    string MatchedHsPrefix,
    bool FromEntryAgencyFallback);
