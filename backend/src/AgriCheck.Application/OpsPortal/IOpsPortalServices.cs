using AgriCheck.Application.ClientPortal.Dtos;
using AgriCheck.Application.OpsPortal.Dtos;

namespace AgriCheck.Application.OpsPortal;

public interface IWarehouseOpsService
{
    Task<WarehouseDashboardDto> GetDashboardAsync(CancellationToken cancellationToken = default);
    Task<PagedResult<WarehouseInventoryListItemDto>> ListInventoryAsync(int page, int pageSize, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ContainerListItemDto>> ListReceivableContainersAsync(CancellationToken cancellationToken = default);
    Task<WarehouseInventoryListItemDto> ReceiveContainerAsync(ReceiveContainerRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ReleaseAuthorizationListItemDto>> ListReleaseAuthorizationsAsync(CancellationToken cancellationToken = default);
    Task<ReleaseAuthorizationListItemDto> CreateReleaseAuthorizationAsync(CreateReleaseAuthorizationRequest request, CancellationToken cancellationToken = default);
    Task<ReleaseRecordListItemDto> ExecuteReleaseAsync(ExecuteReleaseRequest request, CancellationToken cancellationToken = default);
}

public interface IDriverOpsService
{
    Task<DriverDashboardDto> GetDashboardAsync(CancellationToken cancellationToken = default);
    Task<DriverProfileDto?> GetProfileAsync(CancellationToken cancellationToken = default);
    Task<DriverProfileDto> UpdateProfileAsync(UpdateDriverProfileRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ContainerListItemDto>> ListAssignedContainersAsync(CancellationToken cancellationToken = default);
    Task<ContainerListItemDto> UpdateContainerStatusAsync(Guid containerUuid, UpdateContainerStatusRequest request, CancellationToken cancellationToken = default);
    Task RecordContainerLocationAsync(Guid containerUuid, RecordContainerLocationRequest request, CancellationToken cancellationToken = default);
    Task<ContainerTrackDto> GetContainerTrackAsync(Guid containerUuid, CancellationToken cancellationToken = default);
}

public interface IMobileSyncService
{
    Task<MobileSyncPushResultDto> PushAsync(MobileSyncPushRequest request, CancellationToken cancellationToken = default);
    Task<MobileSyncPullResultDto> PullAsync(CancellationToken cancellationToken = default);
}

public interface IOperatorOpsService
{
    Task<IReadOnlyList<ContainerListItemDto>> ListClaimableContainersAsync(CancellationToken cancellationToken = default);
    Task<ContainerListItemDto> ClaimContainerAsync(Guid containerUuid, CancellationToken cancellationToken = default);
    Task<ContainerListItemDto> ClaimContainerByQrAsync(ScanTransportQrRequest request, CancellationToken cancellationToken = default);
    Task<ContainerListItemDto> AssignDriverAsync(Guid containerUuid, AssignDriverRequest request, CancellationToken cancellationToken = default);
}

public interface IDoctorInspectionService
{
    Task<IReadOnlyList<ContainerListItemDto>> ListInspectableContainersAsync(CancellationToken cancellationToken = default);
    Task<ContainerListItemDto> ClaimContainerAsync(Guid containerUuid, CancellationToken cancellationToken = default);
    Task<ContainerListItemDto> CompleteInspectionAsync(Guid containerUuid, CompleteDoctorInspectionRequest request, CancellationToken cancellationToken = default);
}
