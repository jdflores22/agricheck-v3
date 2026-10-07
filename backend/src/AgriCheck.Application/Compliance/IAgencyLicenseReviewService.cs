using AgriCheck.Application.ClientPortal.Dtos;

namespace AgriCheck.Application.Compliance;

public interface IAgencyLicenseReviewService
{
    Task<PagedResult<AgencyLicenseApplicationListItemDto>> ListAsync(int page, int pageSize, CancellationToken cancellationToken = default);

    Task<AgencyLicenseApplicationDetailDto?> GetAsync(Guid uuid, CancellationToken cancellationToken = default);

    Task ApproveAsync(Guid uuid, ApproveAgencyLicenseApplicationRequest request, CancellationToken cancellationToken = default);

    Task RejectAsync(Guid uuid, RejectAgencyLicenseApplicationRequest request, CancellationToken cancellationToken = default);
}
