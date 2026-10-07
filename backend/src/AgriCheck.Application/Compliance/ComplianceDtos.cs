using AgriCheck.Domain.Enums;

namespace AgriCheck.Application.Compliance;

public record ComplianceBlockerDto(string Code, string Message, string? Layer = null);

public record ComplianceLayerDto(
    string Layer,
    string Status,
    string Summary,
    string? Reference = null);

public record ComplianceEvaluationResult(
    bool IsCompliant,
    IReadOnlyList<ComplianceBlockerDto> Blockers,
    IReadOnlyList<ComplianceBlockerDto> Warnings);

public record EntryComplianceChainDto(
    Guid EntryUuid,
    string ReferenceNo,
    string EntryStatus,
    string CompliancePhase,
    long AgencyId,
    string AgencyCode,
    ComplianceLayerDto Accreditation,
    ComplianceLayerDto AgencyLicense,
    ComplianceLayerDto DestinationFacility,
    ComplianceLayerDto SpsImportClearance,
    ComplianceLayerDto Erfi,
    ComplianceLayerDto Mav,
    IReadOnlyList<ComplianceBlockerDto> Blockers,
    IReadOnlyList<ComplianceBlockerDto> Warnings,
    string? NextRecommendedAction);

public record ComplianceGateContext(
    long UserId,
    long EntryId,
    long AgencyId,
    ComplianceGatePhase Phase,
    long? FinalWarehouseFacilityId = null,
    long? CommodityId = null);
