namespace AgriCheck.Application.Compliance;

public interface IFacilityComplianceService
{
    Task<FacilityDestinationStatusDto> GetDestinationStatusAsync(long warehouseFacilityId, CancellationToken cancellationToken = default);

    Task<bool> IsDestinationReadyAsync(long warehouseFacilityId, CancellationToken cancellationToken = default);

    Task<IReadOnlyDictionary<long, FacilityDestinationStatusDto>> GetDestinationStatusesAsync(
        IReadOnlyList<long> warehouseFacilityIds,
        CancellationToken cancellationToken = default);
}

public interface IClientFacilityRegistrationService
{
    Task<IReadOnlyList<ClientFacilityRegistrationListItemDto>> ListAsync(
        long? entryAgencyId = null,
        CancellationToken cancellationToken = default);

    Task<ClientFacilityRegistrationDto?> GetByWarehouseAsync(long warehouseFacilityId, CancellationToken cancellationToken = default);

    Task<ClientFacilityRegistrationDto> UpsertAsync(UpsertFacilityRegistrationRequest request, CancellationToken cancellationToken = default);

    Task<ClientFacilityRegistrationDto> SubmitAsync(Guid registrationUuid, CancellationToken cancellationToken = default);

    Task<FacilityAccreditationSummaryDto> RegisterCswAsync(RegisterFacilityCswRequest request, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ClientFacilityRegistrationListItemDto>> SearchLicensedWarehousesAsync(
        string? query,
        CancellationToken cancellationToken = default);

    Task<ClientFacilityRegistrationListItemDto> LinkWarehouseAsync(long warehouseFacilityId, CancellationToken cancellationToken = default);
}
