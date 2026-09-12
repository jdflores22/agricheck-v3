namespace AgriCheck.Application.AgencyPortal.Dtos;

public record AgencyDashboardDto(
    int QueueCount,
    int MyAssignments,
    int PendingInspections,
    int OpenBillings,
    int PendingAccreditation,
    string AgencyCode,
    string AgencyName);

public record AgencyEntryListItemDto(
    Guid Uuid,
    string ReferenceNo,
    string EntryType,
    string Status,
    string ApplicantName,
    string? CompanyName,
    string? CommodityName,
    string PaymentStatus,
    DateTime? SubmittedAt,
    bool IsAssignedToMe);

public record AgencyEntryEvaluationDto(
    Guid Uuid,
    string ReferenceNo,
    string EntryType,
    string Status,
    string AgencyCode,
    string ApplicantName,
    string? CompanyName,
    string PaymentStatus,
    DateTime? SubmittedAt,
    string? Notes,
    string? FormDataJson,
    string? FormSchemaJson,
    string? FormName,
    bool IsAssignedToMe,
    ClientPortal.Dtos.EntryDetailDto? Detail,
    IReadOnlyList<AgencyEntryFileDto> Files,
    IReadOnlyList<ComplianceItemResultDto> Compliance,
    IReadOnlyList<EvaluatorNoteDto> NotesList,
    IReadOnlyList<ClientPortal.Dtos.StatusHistoryDto> StatusHistory,
    IReadOnlyList<ClientPortal.Dtos.TimelineEventDto> Timeline,
    ClientPortal.Dtos.EntryMavInfoDto? Mav);

public record EvaluateEntryMavRequest(string Decision, string? Remarks);

public record AgencyEntryFileDto(
    Guid Uuid,
    string OriginalFileName,
    string? DocumentType,
    long FileSizeBytes,
    DateTime CreatedAt,
    string? EvaluationDecision,
    string? EvaluationComment,
    IReadOnlyList<ClientPortal.Dtos.EntryFileVersionDto> Versions);

public record ComplianceItemResultDto(
    long ItemId,
    string Label,
    bool IsRequired,
    string Status,
    string? Notes);

public record EvaluatorNoteDto(string Note, bool IsInternal, string AuthorName, DateTime CreatedAt);

public record EvaluateFileRequest(string Decision, string? Comment);

public record UpdateComplianceRequest(IReadOnlyList<ComplianceItemUpdate> Items);

public record ComplianceItemUpdate(long ItemId, string Status, string? Notes);

public record AddEvaluatorNoteRequest(string Note, bool IsInternal);

public record CompleteEvaluationRequest(string Decision, string? Comment);

public record InspectionListItemDto(
    Guid Uuid,
    Guid EntryUuid,
    string EntryReferenceNo,
    string Status,
    string ApplicantName,
    DateTime? ScheduledAt,
    DateTime? CompletedAt);

public record InspectionDto(
    Guid Uuid,
    Guid EntryUuid,
    string EntryReferenceNo,
    string Status,
    string ApplicantName,
    DateTime? ScheduledAt,
    DateTime? CompletedAt,
    string? Findings,
    IReadOnlyList<InspectionPhotoDto> Photos);

public record InspectionPhotoDto(Guid Uuid, string OriginalFileName, string? Caption, DateTime CreatedAt);

public record CreateInspectionRequest(Guid EntryUuid, DateTime? ScheduledAt);

public record UpdateInspectionRequest(string? Findings, string? Status);

public record CompleteInspectionRequest(string Result, string? Findings);

public record AgencyBillingListItemDto(
    Guid Uuid,
    string BillNumber,
    string Description,
    decimal Amount,
    string Status,
    string? EntryReferenceNo,
    DateTime? IssuedAt,
    DateTime? PaidAt);

public record CreateAgencyBillingRequest(Guid EntryUuid, decimal Amount, string Description);

public record VerifyDaBillingPaymentRequest(bool Approved, string? Notes);

public record ReviewContainerInspectionPhotoRequest(string Decision, string? Comment);

public record AddTransportTagRequest(Guid ContainerUuid, string? TransportType);

public record AgencyAccreditationListItemDto(
    Guid Uuid,
    string CompanyName,
    string SubmissionType,
    string Status,
    string DisplayStatus,
    string ApplicantName,
    DateTime? SubmittedAt,
    string? AssignedOfficerName,
    DateTime? ClaimedAt,
    bool IsAssignedToMe);

public record AccreditationOfficerDashboardDto(
    int UnclaimedCount,
    int MyApplicationsCount,
    int ApprovedCount,
    int RejectedCount,
    IReadOnlyList<AgencyAccreditationListItemDto> Unclaimed,
    IReadOnlyList<AgencyAccreditationListItemDto> MyApplications);

public record AgencyAccreditationDetailDto(
    Guid Uuid,
    string CompanyName,
    string SubmissionType,
    string Status,
    string ApplicantName,
    string? FormDataJson,
    string? ReviewComments,
    string? AssignedOfficerName,
    DateTime? ClaimedAt,
    DateTime? SubmittedAt,
    string? AccreditationNumber,
    string? FormSchemaJson,
    string? FormName,
    bool IsAssignedToMe,
    bool CanClaim,
    Guid? CertificateUuid,
    string? CertificateNumber,
    IReadOnlyList<AgencySubmissionFileDto> Files,
    IReadOnlyList<ClientPortal.Dtos.AccreditationHistoryDto> History);

public record CompleteAccreditationReviewResponse(
    string Status,
    string? AccreditationNumber,
    bool CertificateIssued,
    Guid? CertificateUuid,
    string? CertificateNumber,
    string? CertificateMessage);

public record AgencySubmissionFileDto(
    Guid Uuid,
    string OriginalFileName,
    string? ReviewDecision,
    string? ReviewComment,
    long FileSizeBytes,
    DateTime CreatedAt,
    IReadOnlyList<ClientPortal.Dtos.SubmissionFileVersionDto> Versions);

public record ReviewSubmissionFileRequest(string Decision, string? Comment);

public record CompleteAccreditationReviewRequest(string Decision, string? Comment, string? AccreditationNumber);

public record SecretaryReportDto(
    int TotalEntries,
    int SubmittedEntries,
    int UnderReviewEntries,
    int ApprovedEntries,
    int RejectedEntries,
    int CompletedInspections,
    int PendingAccreditation,
    int IssuedBillings,
    int PaidBillings);
