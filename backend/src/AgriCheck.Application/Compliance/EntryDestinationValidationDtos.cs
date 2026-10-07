namespace AgriCheck.Application.Compliance;

public record EntryDestinationValidationRequest(
    long? WarehouseFacilityId,
    decimal? ShipmentQuantity,
    string? ShipmentUnit,
    Guid? ExcludeEntryUuid,
    string? FormDataJson,
    long? EntryAgencyId = null);

public record EntryDestinationCheckDto(
    string Code,
    string Severity,
    string Message,
    string? Detail);

public record EntryDestinationValidationResultDto(
    bool IsValid,
    IReadOnlyList<EntryDestinationCheckDto> Checks,
    EntryDestinationCapacitySnapshotDto? Capacity);

public record EntryDestinationCapacitySnapshotDto(
    long WarehouseFacilityId,
    string WarehouseCode,
    string WarehouseName,
    decimal? FacilityMaxCapacityKg,
    decimal FacilityUsedKg,
    decimal? FacilityHeadroomKg,
    decimal? ContractAllocatedKg,
    decimal ImporterCommittedKg,
    decimal ImporterStoredKg,
    decimal ImporterHeadroomKg,
    decimal IncomingKg,
    string? IncomingCommodityClass,
    IReadOnlyList<string> FacilityAllowedClasses,
    IReadOnlyList<string> ContractAllowedClasses);
