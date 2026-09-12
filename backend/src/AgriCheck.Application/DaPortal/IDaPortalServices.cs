using AgriCheck.Application.DaPortal.Dtos;

namespace AgriCheck.Application.DaPortal;

public interface IDaDashboardService
{
    Task<DaDashboardDto> GetDashboardAsync(CancellationToken cancellationToken = default);
}

public interface IDaAgencyOverviewService
{
    Task<IReadOnlyList<DaAgencySummaryDto>> ListAgenciesAsync(CancellationToken cancellationToken = default);
    Task<DaAgencyOversightDto> GetOversightAsync(long id, CancellationToken cancellationToken = default);
}

public interface IDaOversightReportService
{
    Task<DaOversightReportDto> GetReportAsync(CancellationToken cancellationToken = default);
}

public interface IDaWarehouseManagementService
{
    Task<IReadOnlyList<DaWarehouseListItemDto>> ListAsync(CancellationToken cancellationToken = default);
    Task<DaWarehouseDetailDto> GetDetailAsync(long id, CancellationToken cancellationToken = default);
    Task<DaWarehouseListItemDto> CreateAsync(SaveDaWarehouseRequest request, CancellationToken cancellationToken = default);
    Task<DaWarehouseListItemDto> UpdateAsync(long id, SaveDaWarehouseRequest request, CancellationToken cancellationToken = default);
}
