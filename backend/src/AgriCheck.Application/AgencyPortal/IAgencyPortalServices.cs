using AgriCheck.Application.AgencyPortal.Dtos;
using AgriCheck.Application.ClientPortal.Dtos;
using AgriCheck.Domain.Entities;

namespace AgriCheck.Application.AgencyPortal;

public interface IAgencyDashboardService
{
    Task<AgencyDashboardDto> GetDashboardAsync(CancellationToken cancellationToken = default);
}

public interface IEvaluatorService
{
    Task<PagedResult<AgencyEntryListItemDto>> ListQueueAsync(int page, int pageSize, CancellationToken cancellationToken = default);
    Task<PagedResult<AgencyEntryListItemDto>> ListMyAssignmentsAsync(int page, int pageSize, CancellationToken cancellationToken = default);
    Task AssignToSelfAsync(Guid entryUuid, CancellationToken cancellationToken = default);
    Task<AgencyEntryEvaluationDto?> GetEntryAsync(Guid entryUuid, CancellationToken cancellationToken = default);
    Task EvaluateFileAsync(Guid fileUuid, EvaluateFileRequest request, CancellationToken cancellationToken = default);
    Task EvaluateMavDocumentAsync(Guid entryUuid, EvaluateEntryMavRequest request, CancellationToken cancellationToken = default);
    Task UpdateComplianceAsync(Guid entryUuid, UpdateComplianceRequest request, CancellationToken cancellationToken = default);
    Task AddNoteAsync(Guid entryUuid, AddEvaluatorNoteRequest request, CancellationToken cancellationToken = default);
    Task CompleteEvaluationAsync(Guid entryUuid, CompleteEvaluationRequest request, CancellationToken cancellationToken = default);
    Task<PagedResult<AgencyEntryListItemDto>> ListApprovedEntriesAsync(int page, int pageSize, CancellationToken cancellationToken = default);
    Task<StoredFileDownload> DownloadFileAsync(Guid entryUuid, Guid fileUuid, CancellationToken cancellationToken = default);
    Task<StoredFileDownload> DownloadFileVersionAsync(Guid entryUuid, Guid fileUuid, int versionNumber, CancellationToken cancellationToken = default);
}

public interface IInspectionService
{
    Task<PagedResult<InspectionListItemDto>> ListAsync(int page, int pageSize, string? status, CancellationToken cancellationToken = default);
    Task<InspectionDto?> GetAsync(Guid uuid, CancellationToken cancellationToken = default);
    Task<InspectionDto> CreateAsync(CreateInspectionRequest request, CancellationToken cancellationToken = default);
    Task<InspectionDto> UpdateAsync(Guid uuid, UpdateInspectionRequest request, CancellationToken cancellationToken = default);
    Task<InspectionDto> CompleteAsync(Guid uuid, CompleteInspectionRequest request, CancellationToken cancellationToken = default);
    Task<InspectionPhotoDto> UploadPhotoAsync(Guid inspectionUuid, Stream fileStream, string fileName, string contentType, string? caption, CancellationToken cancellationToken = default);
}

public interface IAgencyBillingService
{
    Task<PagedResult<AgencyBillingListItemDto>> ListAsync(int page, int pageSize, CancellationToken cancellationToken = default);
    Task<AgencyBillingListItemDto> CreateAsync(CreateAgencyBillingRequest request, CancellationToken cancellationToken = default);
    Task<AgencyBillingListItemDto> IssueAsync(Guid uuid, CancellationToken cancellationToken = default);
    Task<AgencyBillingListItemDto> MarkPaidAsync(Guid uuid, CancellationToken cancellationToken = default);
    Task<AgencyBillingListItemDto> VerifyPaymentAsync(Guid uuid, VerifyDaBillingPaymentRequest request, CancellationToken cancellationToken = default);
}

public interface IAccreditationReviewService
{
    Task<AccreditationOfficerDashboardDto> GetDashboardAsync(CancellationToken cancellationToken = default);
    Task<PagedResult<AgencyAccreditationListItemDto>> ListAsync(int page, int pageSize, string? filter, CancellationToken cancellationToken = default);
    Task<AgencyAccreditationDetailDto?> GetAsync(Guid uuid, CancellationToken cancellationToken = default);
    Task ClaimAsync(Guid uuid, CancellationToken cancellationToken = default);
    Task ReleaseAsync(Guid uuid, CancellationToken cancellationToken = default);
    Task ReviewFileAsync(Guid fileUuid, ReviewSubmissionFileRequest request, CancellationToken cancellationToken = default);
    Task<CompleteAccreditationReviewResponse> CompleteReviewAsync(Guid uuid, CompleteAccreditationReviewRequest request, CancellationToken cancellationToken = default);
    Task<StoredFileDownload> DownloadFileAsync(Guid submissionUuid, Guid fileUuid, CancellationToken cancellationToken = default);
    Task<StoredFileDownload> DownloadFileVersionAsync(Guid submissionUuid, Guid fileUuid, int versionNumber, CancellationToken cancellationToken = default);
}

public interface IAccreditationCertificateService
{
    Task<AccreditationCertificateIssueResult> TryIssueForApprovedSubmissionAsync(
        AccreditationSubmission submission,
        long issuedByUserId,
        CancellationToken cancellationToken = default);

    Task<AccreditationCertificateIssueResult> RegenerateForApprovedSubmissionAsync(
        Guid submissionUuid,
        long issuedByUserId,
        CancellationToken cancellationToken = default);
}

public record AccreditationCertificateIssueResult(
    bool Issued,
    Guid? CertificateUuid,
    string? CertificateNumber,
    string? Message);

public interface ISecretaryReportService
{
    Task<SecretaryReportDto> GetReportAsync(CancellationToken cancellationToken = default);
}
