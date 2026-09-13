using System.Text.Json;
using AgriCheck.Application.AdminPortal.Dtos;
using AgriCheck.Application.ClientPortal.Dtos;

namespace AgriCheck.Application.AdminPortal;

public interface IAdminDashboardService
{
    Task<AdminDashboardDto> GetDashboardAsync(CancellationToken cancellationToken = default);
}

public interface IAdminUserService
{
    Task<PagedResult<AdminUserListItemDto>> ListAsync(int page, int pageSize, CancellationToken cancellationToken = default);
    Task<AdminUserDetailDto?> GetAsync(Guid uuid, CancellationToken cancellationToken = default);
    Task<AdminUserListItemDto> CreateAsync(CreateAdminUserRequest request, CancellationToken cancellationToken = default);
    Task<AdminUserListItemDto> UpdateAsync(Guid uuid, UpdateAdminUserRequest request, CancellationToken cancellationToken = default);
}

public interface IAdminAgencyService
{
    Task<IReadOnlyList<AdminAgencyDto>> ListAsync(CancellationToken cancellationToken = default);
    Task<AdminAgencyDetailDto?> GetAsync(long id, CancellationToken cancellationToken = default);
    Task<AdminAgencyDto> CreateAsync(CreateAgencyRequest request, CancellationToken cancellationToken = default);
    Task<AdminAgencyDto> UpdateAsync(long id, UpdateAgencyRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(long id, CancellationToken cancellationToken = default);
    Task<AdminAgencyDetailDto> UploadLogoAsync(long id, Stream fileStream, string fileName, string contentType, CancellationToken cancellationToken = default);
    Task<AdminAgencyDetailDto> AssignSecretaryAsync(long id, AssignAgencySecretaryRequest request, CancellationToken cancellationToken = default);
    Task<AdminAgencyDetailDto> AssignUndersecretariesAsync(long id, AssignAgencyUndersecretariesRequest request, CancellationToken cancellationToken = default);
}

public interface IAdminCommodityService
{
    Task<IReadOnlyList<AdminCommodityDto>> ListAsync(CancellationToken cancellationToken = default);
    Task<AdminCommodityDto> CreateAsync(CreateCommodityRequest request, CancellationToken cancellationToken = default);
    Task<AdminCommodityDto> UpdateAsync(long id, UpdateCommodityRequest request, CancellationToken cancellationToken = default);
}

public interface IAdminPaymentConfigService
{
    Task<IReadOnlyList<ProcessingFeeConfigDto>> ListAsync(CancellationToken cancellationToken = default);
    Task<ProcessingFeeConfigDto> UpsertAsync(UpsertProcessingFeeRequest request, CancellationToken cancellationToken = default);
    Task<AdminPaymentSettingsDto> GetSettingsAsync(CancellationToken cancellationToken = default);
    Task<AdminPaymentSettingsDto> UpdateSettingsAsync(UpdateAdminPaymentSettingsRequest request, CancellationToken cancellationToken = default);
}

public interface IAdminEntryPaymentService
{
    Task<PagedResult<AdminEntryPaymentListItemDto>> ListAsync(int page, int pageSize, string? status, CancellationToken cancellationToken = default);
    Task<AdminRevenueSummaryDto> GetRevenueSummaryAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AdminPendingEntryCashPaymentDto>> ListPendingCashPaymentsAsync(CancellationToken cancellationToken = default);
    Task VerifyCashPaymentAsync(Guid billUuid, VerifyAdminEntryCashPaymentRequest request, CancellationToken cancellationToken = default);
}

public interface IFormBuilderService
{
    Task<IReadOnlyList<FormTemplateListItemDto>> ListAsync(CancellationToken cancellationToken = default);
    Task<FormTemplateDetailDto?> GetAsync(Guid uuid, CancellationToken cancellationToken = default);
    Task<FormTemplateDetailDto> SaveAsync(Guid? uuid, SaveFormTemplateRequest request, CancellationToken cancellationToken = default);
    Task<FormTemplateDetailDto> SetActiveAsync(Guid uuid, bool isActive, CancellationToken cancellationToken = default);
    Task<FormTemplateDetailDto> CloneAsync(Guid uuid, CloneFormTemplateRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid uuid, CancellationToken cancellationToken = default);
    Task<string> ExportAsync(Guid uuid, CancellationToken cancellationToken = default);
    Task<FormTemplateDetailDto> ImportAsync(JsonElement payload, CancellationToken cancellationToken = default);
}

public interface ICertificateTemplateService
{
    Task<IReadOnlyList<CertificateTemplateListItemDto>> ListAsync(CancellationToken cancellationToken = default);
    Task<CertificateTemplateDetailDto?> GetAsync(Guid uuid, CancellationToken cancellationToken = default);
    Task<CertificateTemplateDetailDto> SaveAsync(Guid? uuid, SaveCertificateTemplateRequest request, CancellationToken cancellationToken = default);
    Task<CertificateTemplateDetailDto> CloneAsync(Guid uuid, CloneFormTemplateRequest request, CancellationToken cancellationToken = default);
    Task<CertificateTemplateDetailDto> SetActiveAsync(Guid uuid, bool isActive, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid uuid, CancellationToken cancellationToken = default);
    Task<string> ExportAsync(Guid uuid, CancellationToken cancellationToken = default);
    Task<CertificateTemplateDetailDto> ImportAsync(JsonElement payload, CancellationToken cancellationToken = default);
    Task<byte[]> PreviewAsync(Guid uuid, CancellationToken cancellationToken = default);
    Task<string> UploadImageAsync(Guid uuid, Stream fileStream, string fileName, CancellationToken cancellationToken = default);
}

public interface ICertificateIssuanceService
{
    Task<PagedResult<AdminCertificateListItemDto>> ListAsync(int page, int pageSize, CancellationToken cancellationToken = default);
    Task<AdminCertificateListItemDto> IssueAsync(IssueCertificateRequest request, CancellationToken cancellationToken = default);
    Task<AdminCertificateListItemDto> RevokeAsync(Guid uuid, RevokeCertificateRequest request, CancellationToken cancellationToken = default);
    Task<byte[]?> DownloadPdfAsync(Guid uuid, CancellationToken cancellationToken = default);
    Task<PagedResult<AdminApprovedEntryDto>> ListApprovedEntriesAsync(int page, int pageSize, CancellationToken cancellationToken = default);
}

public interface IAdminAuditService
{
    Task<PagedResult<AuditLogListItemDto>> ListAsync(int page, int pageSize, CancellationToken cancellationToken = default);
}

public interface IAdminRoleService
{
    Task<IReadOnlyList<AdminRoleDto>> ListAsync(CancellationToken cancellationToken = default);
}

public interface IAdminSettingsService
{
    Task<AdminSettingsDto> GetAsync(CancellationToken cancellationToken = default);
    Task<AdminSettingsDto> UpdateAsync(UpdateAdminSettingsRequest request, CancellationToken cancellationToken = default);
    Task<AdminSettingsDto> UploadBrandingAssetAsync(string assetType, Stream fileStream, string fileName, string contentType, CancellationToken cancellationToken = default);
}
