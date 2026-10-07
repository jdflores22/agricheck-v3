namespace AgriCheck.Application.Compliance;

public record EntrySpsReadinessDto(
    Guid EntryUuid,
    string? HsCode,
    SpsIssuingAgencyMatchDto? SuggestedIssuer,
    bool AgencyMatchesEntry,
    IReadOnlyList<SpsChecklistItemDto> Checklist,
    bool AllRequiredDocumentsReady);

public record SpsChecklistItemDto(
    string Code,
    string Title,
    bool IsRequired,
    string Status,
    long? EntryFileId);

public record EntryErfiSubmissionDto(
    Guid Uuid,
    Guid EntryUuid,
    string Status,
    DateTime? FiledAt,
    DateTime? EtaAtPoe,
    string? BolReference,
    Guid ClearanceUuid,
    string ClearanceNumber);

public record UpsertEntryErfiRequest(
    DateTime? EtaAtPoe,
    string? BolReference,
    string? IfmDestinationJson);

public record VerifyFacilityRegistrationRequest(bool Approve, string? ReviewComments);
