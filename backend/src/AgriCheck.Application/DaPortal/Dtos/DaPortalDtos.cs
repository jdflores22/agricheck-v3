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
    string? ReceivedByName);

public record DaWarehouseDetailDto(
    DaWarehouseListItemDto Warehouse,
    int StoredContainers,
    int ReleasedContainers,
    int PendingBookings,
    int Capacity,
    decimal UtilizationPercent,
    IReadOnlyList<DaWarehouseInventoryItemDto> Inventory);
