namespace AgriCheck.Application.Compliance;

public interface IClientAgencyLicenseService
{
    Task<IReadOnlyList<AgencyLicenseTypeOptionDto>> ListLicenseTypesAsync(long? agencyId, CancellationToken cancellationToken = default);

    Task<ClientAgencyLicenseOverviewDto> GetOverviewAsync(CancellationToken cancellationToken = default);

    Task<ClientAgencyLicenseApplicationDto?> GetApplicationAsync(Guid uuid, CancellationToken cancellationToken = default);

    Task<ClientAgencyLicenseApplicationDto> CreateApplicationAsync(CreateAgencyLicenseApplicationRequest request, CancellationToken cancellationToken = default);

    Task<ClientAgencyLicenseApplicationDto> UpdateApplicationAsync(Guid uuid, UpdateAgencyLicenseApplicationRequest request, CancellationToken cancellationToken = default);

    Task<ClientAgencyLicenseApplicationDto> SubmitApplicationAsync(Guid uuid, CancellationToken cancellationToken = default);

    Task<ClientAgencyLicenseListItemDto> RegisterExistingLicenseAsync(RegisterClientAgencyLicenseRequest request, CancellationToken cancellationToken = default);
}
