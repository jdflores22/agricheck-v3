using AgriCheck.Application.ClientPortal.Dtos;

namespace AgriCheck.Application.ClientPortal;

public interface IEntryService
{
    Task<PagedResult<EntryListItemDto>> ListAsync(int page, int pageSize, string? status, CancellationToken cancellationToken = default);
    Task<EntryDto?> GetAsync(Guid uuid, CancellationToken cancellationToken = default);
    Task<EntryDto> CreateAsync(CreateEntryRequest request, CancellationToken cancellationToken = default);
    Task<EntryDto> UpdateAsync(Guid uuid, UpdateEntryRequest request, CancellationToken cancellationToken = default);
    Task<EntryDto> SubmitAsync(Guid uuid, CancellationToken cancellationToken = default);
    Task<CheckMavNoResultDto> CheckMavNoAsync(string mavNo, Guid? excludeEntryUuid, CancellationToken cancellationToken = default);
    Task<EntryDto> UpdateMavNoAsync(Guid uuid, UpdateEntryMavRequest request, CancellationToken cancellationToken = default);
    Task<EntryDto> UtilizeMicAsync(Guid uuid, UtilizeEntryMicRequest request, CancellationToken cancellationToken = default);
    Task<EntryFileDto> UploadFileAsync(Guid entryUuid, Stream fileStream, string fileName, string contentType, string? documentType, CancellationToken cancellationToken = default);
    Task<EntryFileDto> UploadComplianceFileAsync(Guid entryUuid, Guid fileUuid, Stream fileStream, string fileName, string contentType, CancellationToken cancellationToken = default);
    Task<EntryDto> ResubmitComplianceAsync(Guid entryUuid, CancellationToken cancellationToken = default);
    Task<StoredFileDownload> DownloadFileAsync(Guid entryUuid, Guid fileUuid, CancellationToken cancellationToken = default);
}

public interface IAccreditationService
{
    Task<PagedResult<AccreditationListItemDto>> ListAsync(int page, int pageSize, CancellationToken cancellationToken = default);
    Task<AccreditationSubmissionDto?> GetAsync(Guid uuid, CancellationToken cancellationToken = default);
    Task<AccreditationSubmissionDto> CreateAsync(CreateAccreditationRequest request, CancellationToken cancellationToken = default);
    Task<AccreditationSubmissionDto> UpdateAsync(Guid uuid, UpdateAccreditationRequest request, CancellationToken cancellationToken = default);
    Task<AccreditationSubmissionDto> SubmitAsync(Guid uuid, CancellationToken cancellationToken = default);
    Task<SubmissionFileDto> UploadFileAsync(Guid submissionUuid, Stream fileStream, string fileName, string contentType, CancellationToken cancellationToken = default);
    Task<SubmissionFileDto> UploadComplianceFileAsync(Guid submissionUuid, Guid fileUuid, Stream fileStream, string fileName, string contentType, CancellationToken cancellationToken = default);
    Task<AccreditationSubmissionDto> ResubmitComplianceAsync(Guid submissionUuid, CancellationToken cancellationToken = default);
    Task<StoredFileDownload> DownloadFileAsync(Guid submissionUuid, Guid fileUuid, CancellationToken cancellationToken = default);
    Task<StoredFileDownload> DownloadFileVersionAsync(Guid submissionUuid, Guid fileUuid, int versionNumber, CancellationToken cancellationToken = default);
}

public interface ICertificateService
{
    Task<PagedResult<CertificateListItemDto>> ListAsync(int page, int pageSize, CancellationToken cancellationToken = default);
    Task<CertificateDto?> GetAsync(Guid uuid, CancellationToken cancellationToken = default);
    Task<StoredFileDownload?> GetPdfAsync(Guid uuid, CancellationToken cancellationToken = default);
    Task<CertificateVerifyDto?> VerifyAsync(string verificationCode, CancellationToken cancellationToken = default);
}

public interface IWarehouseBookingService
{
    Task<IReadOnlyList<WarehouseFacilityDto>> ListFacilitiesAsync(CancellationToken cancellationToken = default);
    Task<PagedResult<WarehouseBookingListItemDto>> ListBookingsAsync(int page, int pageSize, CancellationToken cancellationToken = default);
    Task<WarehouseBookingDto?> GetBookingAsync(Guid uuid, CancellationToken cancellationToken = default);
    Task<WarehouseBookingDto> CreateBookingAsync(CreateWarehouseBookingRequest request, CancellationToken cancellationToken = default);
    Task CancelBookingAsync(Guid uuid, CancellationToken cancellationToken = default);
}

public interface IClientBillService
{
    Task<IReadOnlyList<ClientBillSummaryDto>> ListForUserAsync(CancellationToken cancellationToken = default);
    Task<ClientBillDto?> GetAsync(Guid uuid, CancellationToken cancellationToken = default);
    Task<ClientBillDto?> GetByPaymentTokenAsync(string token, CancellationToken cancellationToken = default);
    Task<ClientBillDto> PayAsync(Guid uuid, PayBillRequest request, CancellationToken cancellationToken = default);
    Task<ClientBillDto> PayByTokenAsync(string token, PayBillRequest request, CancellationToken cancellationToken = default);
    Task<BillPaymentOptionsDto> GetPaymentOptionsAsync(Guid uuid, CancellationToken cancellationToken = default);
    Task<InitiateBillPaymentResultDto> InitiatePaymentAsync(Guid uuid, PayBillRequest request, CancellationToken cancellationToken = default);
    Task<InitiateBillPaymentResultDto> InitiatePaymentByTokenAsync(string token, PayBillRequest request, CancellationToken cancellationToken = default);
    Task<ClientBillDto> ConfirmPaymentAsync(Guid uuid, CancellationToken cancellationToken = default);
    Task<ClientBillDto> ConfirmPaymentByTokenAsync(string token, CancellationToken cancellationToken = default);
    Task CompletePaymentByReferenceAsync(string paymentReference, string? gatewayTransactionId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ClientPaymentHistoryItemDto>> ListPaymentHistoryAsync(CancellationToken cancellationToken = default);
    Task<byte[]?> GetReceiptPdfAsync(Guid billUuid, CancellationToken cancellationToken = default);
}

public interface IClientFormService
{
    Task<IReadOnlyList<ClientFormListItemDto>> ListPublishedAsync(long? agencyId, string formType, CancellationToken cancellationToken = default);
    Task<ClientFormDetailDto?> GetPublishedAsync(Guid uuid, CancellationToken cancellationToken = default);
}

public interface IClientProfileService
{
    Task<ClientProfileDto> GetAsync(CancellationToken cancellationToken = default);
    Task<ClientProfileDto> UpdateAsync(UpdateClientProfileRequest request, CancellationToken cancellationToken = default);
}

public interface IClientContainerService
{
    Task<IReadOnlyList<ClientContainerListItemDto>> ListAsync(string? status, CancellationToken cancellationToken = default);
    Task<ClientContainerDetailDto?> GetAsync(Guid uuid, CancellationToken cancellationToken = default);
}

public interface IClientInspectionService
{
    Task<IReadOnlyList<ClientInspectionListItemDto>> ListAsync(string? status, Guid? entryUuid, CancellationToken cancellationToken = default);
    Task<ClientInspectionDetailDto?> GetAsync(Guid uuid, CancellationToken cancellationToken = default);
}

public interface ICommodityService
{
    Task<IReadOnlyList<CommodityDto>> ListAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AgencyOptionDto>> ListAgenciesAsync(CancellationToken cancellationToken = default);
}

public interface IClientDashboardService
{
    Task<ClientDashboardDto> GetDashboardAsync(CancellationToken cancellationToken = default);
}
