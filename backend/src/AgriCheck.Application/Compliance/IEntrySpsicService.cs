using AgriCheck.Application.ClientPortal.Dtos;

namespace AgriCheck.Application.Compliance;

public interface IEntrySpsicService
{
    Task<EntrySpsApplicationDto?> GetForEntryAsync(Guid entryUuid, CancellationToken cancellationToken = default);

    Task<EntrySpsApplicationDto> CreateApplicationAsync(Guid entryUuid, CancellationToken cancellationToken = default);

    Task<EntrySpsApplicationDto> SubmitApplicationAsync(Guid entryUuid, CancellationToken cancellationToken = default);

    Task<EntrySpsReadinessDto> GetReadinessAsync(Guid entryUuid, CancellationToken cancellationToken = default);

    Task<EntrySpsReadinessDto> AttachDocumentAsync(
        Guid entryUuid,
        string documentCode,
        Guid entryFileUuid,
        CancellationToken cancellationToken = default);
}

public record AttachSpsDocumentRequest(Guid EntryFileUuid);

public interface IEntryErfiService
{
    Task<EntryErfiSubmissionDto?> GetForEntryAsync(Guid entryUuid, CancellationToken cancellationToken = default);

    Task<EntryErfiSubmissionDto> UpsertDraftAsync(Guid entryUuid, UpsertEntryErfiRequest request, CancellationToken cancellationToken = default);

    Task<EntryErfiSubmissionDto> FileAsync(Guid entryUuid, CancellationToken cancellationToken = default);
}

public interface IAgencySpsicReviewService
{
    Task<PagedResult<AgencySpsApplicationListItemDto>> ListAsync(int page, int pageSize, CancellationToken cancellationToken = default);

    Task<EntrySpsApplicationDto?> GetAsync(Guid applicationUuid, CancellationToken cancellationToken = default);

    Task<EntrySpsClearanceDto> IssueClearanceAsync(Guid applicationUuid, IssueSpsClearanceRequest request, CancellationToken cancellationToken = default);
}
