namespace AgriCheck.Application.DaPortal.Dtos;

public record DaDashboardDto(
    int ActiveAgencies,
    int TotalAgencies,
    int TotalEntries,
    int PendingEntries,
    int ApprovedEntries,
    int PendingAccreditation,
    int OpenBillings,
    int CompletedInspections);

public record DaAgencySummaryDto(
    long Id,
    string Code,
    string Name,
    bool IsActive,
    long? ParentId,
    string? LogoUrl,
    int TotalEntries,
    int PendingEntries,
    int PendingAccreditation,
    int OpenBillings,
    DaAgencyLeadershipUserDto? Secretary,
    IReadOnlyList<DaAgencyLeadershipUserDto> Undersecretaries);

public record DaAgencyLeadershipUserDto(
    Guid UserUuid,
    string FullName,
    string Email);

public record DaOversightReportDto(
    int TotalEntries,
    int SubmittedEntries,
    int UnderReviewEntries,
    int ApprovedEntries,
    int RejectedEntries,
    int CompletedInspections,
    int PendingAccreditation,
    int IssuedBillings,
    int PaidBillings);

public record DaAgencyRecentEntryDto(
    Guid Uuid,
    string ReferenceNo,
    string Status,
    DateTime CreatedAt,
    string? SubmitterName);

public record DaAgencyOversightDto(
    DaAgencySummaryDto Agency,
    DaOversightReportDto Report,
    bool IsParentAgency,
    DaAgencySummaryDto? ParentAgency,
    IReadOnlyList<DaAgencySummaryDto> ChildAgencies,
    int ActiveUsers,
    int OpenBillings,
    IReadOnlyList<DaAgencyRecentEntryDto> RecentEntries);

public record DaWarehouseListItemDto(
    long Id,
    string Code,
    string Name,
    bool IsActive,
    int Capacity,
    long? RegionId,
    string? RegionName,
    long? ProvinceId,
    string? ProvinceName,
    long? CityId,
    string? CityName,
    long? BarangayId,
    string? BarangayName,
    string? StreetAddress,
    string? ZipCode,
    string FormattedAddress,
    decimal? Latitude,
    decimal? Longitude);

public record SaveDaWarehouseRequest(
    string Code,
    string Name,
    int Capacity,
    long RegionId,
    long ProvinceId,
    long CityId,
    long BarangayId,
    string? StreetAddress,
    string? ZipCode,
    decimal? Latitude,
    decimal? Longitude,
    bool IsActive);

public record DaWarehouseInventoryItemDto(
    Guid Uuid,
    string ContainerNumber,
    string? ContainerType,
    string EntryReference,
    string? LocationCode,
    string Status,
    DateTime ReceivedAt,
    string? ReceivedByName,
    string? HsCode,
    string? CommodityName,
    decimal? VolumeKg);

public record DaWarehouseDetailDto(
    DaWarehouseListItemDto Warehouse,
    int StoredContainers,
    int ReleasedContainers,
    int PendingBookings,
    int Capacity,
    decimal UtilizationPercent,
    decimal TotalVolumeKg,
    IReadOnlyList<DaGeoStockCommodityRowDto> Commodities,
    IReadOnlyList<DaWarehouseInventoryItemDto> Inventory);

public record DaMavAgencyRowDto(
    string AgencyCode,
    int ActiveLicenses,
    decimal AwardedVolume,
    decimal UtilizedVolume,
    decimal RemainingVolume,
    decimal RegularVolume,
    decimal CombinedVolume);

public record DaMavCommodityRowDto(
    string HsCode,
    string CommodityName,
    string? AgencyCode,
    int ApplicationCount,
    int ActiveLicenses,
    decimal AwardedVolume,
    decimal UtilizedVolume,
    decimal RemainingVolume,
    decimal RegularVolume,
    decimal CombinedVolume);

public record DaMavNationalReportDto(
    int MavYear,
    int OpenPeriods,
    int TotalApplications,
    int ApprovedApplications,
    int ActiveLicenses,
    int IssuedMics,
    decimal TotalAwardedVolume,
    decimal TotalUtilizedVolume,
    decimal TotalRemainingVolume,
    int RegularImportCount,
    decimal TotalRegularVolume,
    decimal TotalCombinedVolume,
    IReadOnlyList<DaMavAgencyRowDto> ByAgency,
    IReadOnlyList<DaMavCommodityRowDto> ByCommodity);

public record DaGeoStockQuery(
    long? RegionId = null,
    long? ProvinceId = null,
    long? CityId = null,
    long? BarangayId = null,
    string? HsCode = null,
    string? CommodityName = null);

public record DaGeoStockBreadcrumbDto(
    string Level,
    long? Id,
    string Name);

public record DaGeoStockCommodityRowDto(
    string HsCode,
    string CommodityName,
    decimal VolumeKg,
    int StoredContainers,
    int WarehouseCount);

public record DaGeoStockLocationRowDto(
    string Level,
    long? Id,
    string Name,
    decimal VolumeKg,
    int WarehouseCount,
    int StoredContainers,
    int Capacity,
    decimal UtilizationPercent,
    IReadOnlyList<DaGeoStockCommodityRowDto> TopCommodities);

public record DaGeoStockWarehouseRowDto(
    long Id,
    string Code,
    string Name,
    decimal VolumeKg,
    int StoredContainers,
    int Capacity,
    decimal UtilizationPercent,
    string? BarangayName,
    string? TopCommodityName,
    string? TopCommodityHsCode);

public record DaGeoStockReportDto(
    string Level,
    string ScopeLabel,
    decimal TotalVolumeKg,
    int WarehouseCount,
    int StoredContainers,
    int Capacity,
    decimal UtilizationPercent,
    IReadOnlyList<DaGeoStockBreadcrumbDto> Path,
    IReadOnlyList<DaGeoStockCommodityRowDto> Commodities,
    IReadOnlyList<DaGeoStockLocationRowDto> Locations,
    IReadOnlyList<DaGeoStockWarehouseRowDto> Warehouses);

public record DaCommodityStockQuery(string? HsCode = null, string? CommodityName = null, string? AgencyCode = null);

public record DaCommodityStockRowDto(
    string HsCode,
    string CommodityName,
    decimal StockKg,
    decimal MavStockKg,
    decimal RegularStockKg,
    int StoredContainers,
    int WarehouseCount,
    int AgencyCount,
    int LinkedImportEntries);

public record DaCommodityStockAgencyRowDto(
    string AgencyCode,
    string HsCode,
    string CommodityName,
    decimal StockKg,
    decimal MavStockKg,
    decimal RegularStockKg,
    int StoredContainers,
    int WarehouseCount);

public record DaCommodityStockReportDto(
    decimal TotalStockKg,
    decimal TotalMavStockKg,
    decimal TotalRegularStockKg,
    int CommodityCount,
    int StoredContainers,
    int WarehouseCount,
    IReadOnlyList<DaCommodityStockRowDto> Commodities,
    IReadOnlyList<DaCommodityStockAgencyRowDto> ByAgency);

public record DaImportPipelineQuery(string? HsCode = null, string? CommodityName = null, string? AgencyCode = null);

public record DaImportPipelineStageRowDto(
    string Stage,
    string Label,
    decimal ExpectedKg,
    int ContainerCount,
    int EntryCount);

public record DaImportPipelineCommodityRowDto(
    string HsCode,
    string CommodityName,
    decimal ExpectedKg,
    decimal ActualKg,
    decimal ProcessingKg,
    decimal InTransitKg,
    decimal AwaitingStorageKg,
    int ExpectedContainers,
    int ActualContainers,
    int ImporterCount,
    int WarehouseCount);

public record DaImportPipelineImporterRowDto(
    Guid ImporterUuid,
    string ImporterName,
    string? CompanyName,
    decimal ExpectedKg,
    decimal ActualKg,
    int EntryCount,
    int CommodityCount);

public record DaImportPipelineCommodityImporterRowDto(
    string HsCode,
    string CommodityName,
    Guid ImporterUuid,
    string ImporterName,
    string? CompanyName,
    decimal ExpectedKg,
    decimal ActualKg,
    int ContainerCount);

public record DaImportPipelineCommodityWarehouseRowDto(
    string HsCode,
    string CommodityName,
    long WarehouseId,
    string WarehouseCode,
    string WarehouseName,
    string? RegionName,
    decimal ActualKg,
    int ContainerCount);

public record DaImportPipelineEntryRowDto(
    Guid EntryUuid,
    string ReferenceNo,
    string AgencyCode,
    string EntryStatus,
    string PipelineStage,
    string PipelineLabel,
    string CommodityName,
    string HsCode,
    decimal ExpectedKg,
    int TotalContainers,
    int StoredContainers,
    int PendingContainers,
    DateTime? SubmittedAt,
    Guid ImporterUuid,
    string ImporterName,
    string? CompanyName);

public record DaImportPipelineReportDto(
    decimal TotalExpectedKg,
    decimal TotalActualKg,
    int ExpectedContainers,
    int ActualContainers,
    int PipelineEntryCount,
    IReadOnlyList<DaImportPipelineStageRowDto> ByStage,
    IReadOnlyList<DaImportPipelineCommodityRowDto> ByCommodity,
    IReadOnlyList<DaImportPipelineImporterRowDto> ByImporter,
    IReadOnlyList<DaImportPipelineCommodityImporterRowDto> ByCommodityImporter,
    IReadOnlyList<DaImportPipelineCommodityWarehouseRowDto> ByCommodityWarehouse,
    IReadOnlyList<DaImportPipelineEntryRowDto> Entries);
