namespace AgriCheck.Application.OpsPortal;

public record WarehouseImporterContractListItemDto(
    Guid? AllocationUuid,
    long WarehouseFacilityId,
    string WarehouseCode,
    string WarehouseName,
    Guid ImporterUserUuid,
    string ImporterEmail,
    string ImporterDisplayName,
    bool IsLinkedImporter,
    string? ContractReference,
    decimal? AllocatedCapacityKg,
    decimal CommittedKg,
    decimal? HeadroomKg,
    IReadOnlyList<string> AllowedCommodityClasses,
    string? AllocationStatus,
    DateTime? ValidFrom,
    DateTime? ValidTo,
    IReadOnlyList<string> FacilityAllowedCommodityClasses,
    string? FacilityScopeHint,
    string? CswLeadAgencyCode);

public record UpsertWarehouseImporterContractRequest(
    Guid? AllocationUuid,
    long WarehouseFacilityId,
    Guid ImporterUserUuid,
    string ContractReference,
    decimal AllocatedCapacityKg,
    IReadOnlyList<string> AllowedCommodityClasses,
    string? Status);
