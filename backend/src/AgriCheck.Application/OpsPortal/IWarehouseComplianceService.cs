using AgriCheck.Application.Compliance;

namespace AgriCheck.Application.OpsPortal;

public interface IWarehouseComplianceService
{
    Task<IReadOnlyList<WarehouseComplianceFacilityListItemDto>> ListFacilitiesAsync(CancellationToken cancellationToken = default);

    Task<WarehouseComplianceDetailDto> GetDetailAsync(long warehouseFacilityId, CancellationToken cancellationToken = default);

    Task<WarehouseLtoApplicationDto> SubmitLtoApplicationAsync(
        SubmitWarehouseLtoApplicationRequest request,
        CancellationToken cancellationToken = default);

    Task<ClientFacilityRegistrationDto> UpsertRsasAsync(
        UpsertFacilityRegistrationRequest request,
        CancellationToken cancellationToken = default);

    Task<ClientFacilityRegistrationDto> SubmitRsasAsync(Guid registrationUuid, CancellationToken cancellationToken = default);
}

public interface IDaWarehouseLtoApplicationService
{
    Task<IReadOnlyList<DaWarehouseLtoApplicationListItemDto>> ListPendingAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DaWarehouseLtoApplicationListItemDto>> ListRecentAsync(
        int limit = 50,
        CancellationToken cancellationToken = default);

    Task<WarehouseLtoApplicationDto> ResolveAsync(
        Guid applicationUuid,
        ResolveWarehouseLtoApplicationRequest request,
        CancellationToken cancellationToken = default);
}
