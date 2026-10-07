namespace AgriCheck.Application.OpsPortal;

public interface IWarehouseImporterContractService
{
    Task<IReadOnlyList<WarehouseImporterContractListItemDto>> ListAsync(
        long? warehouseFacilityId,
        CancellationToken cancellationToken = default);

    Task<WarehouseImporterContractListItemDto> UpsertAsync(
        UpsertWarehouseImporterContractRequest request,
        CancellationToken cancellationToken = default);
}
