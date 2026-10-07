namespace AgriCheck.Application.Compliance;

public record EntrySpsApplicationDto(
    Guid Uuid,
    Guid EntryUuid,
    string Status,
    DateTime? SubmittedAt,
    string? ReviewComments,
    EntrySpsClearanceDto? IssuedClearance);

public record EntrySpsClearanceDto(
    Guid Uuid,
    string ClearanceNumber,
    string Status,
    DateTime IssuedAt,
    DateTime? MustShipOutBy,
    DateTime? MustArriveBy,
    string? DaTradeReference);

public record IssueSpsClearanceRequest(
    string ClearanceNumber,
    DateTime IssuedAt,
    DateTime? MustShipOutBy,
    DateTime? MustArriveBy,
    string? DaTradeReference,
    string? ReviewComments);

public record AgencySpsApplicationListItemDto(
    Guid Uuid,
    Guid EntryUuid,
    string ReferenceNo,
    string ImporterName,
    string Status,
    DateTime? SubmittedAt);
