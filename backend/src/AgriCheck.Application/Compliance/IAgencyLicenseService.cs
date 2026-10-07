namespace AgriCheck.Application.Compliance;

public interface IAgencyLicenseService
{
    Task<bool> HasActiveSpsicPrerequisiteLicenseAsync(long userId, long agencyId, CancellationToken cancellationToken = default);

    Task<AgencyLicenseSummaryDto?> GetPrimaryActiveLicenseAsync(long userId, long agencyId, CancellationToken cancellationToken = default);

    Task<bool> MeetsCommodityLicenseRulesAsync(
        long userId,
        long agencyId,
        long? commodityId,
        string? hsCode,
        CancellationToken cancellationToken = default);
}

public record AgencyLicenseSummaryDto(
    Guid Uuid,
    string LicenseNumber,
    string LicenseTypeCode,
    string LicenseTypeName,
    DateTime ExpiresAt);
