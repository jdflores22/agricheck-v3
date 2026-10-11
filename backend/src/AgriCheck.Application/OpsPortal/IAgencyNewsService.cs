namespace AgriCheck.Application.OpsPortal;

public interface IAgencyNewsService
{
    Task<IReadOnlyList<AgencyNewsListItemDto>> ListPublishedAsync(
        string? category,
        string? search,
        CancellationToken cancellationToken = default);

    Task<AgencyNewsDetailDto?> GetPublishedAsync(Guid uuid, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AgencyNewsManageListItemDto>> ListForAgencyAsync(CancellationToken cancellationToken = default);

    Task<AgencyNewsManageDetailDto?> GetForAgencyAsync(Guid uuid, CancellationToken cancellationToken = default);

    Task<AgencyNewsManageDetailDto> CreateDraftAsync(
        CreateAgencyNewsRequest request,
        CancellationToken cancellationToken = default);

    Task<AgencyNewsManageDetailDto> UpdateAsync(
        Guid uuid,
        UpdateAgencyNewsRequest request,
        CancellationToken cancellationToken = default);

    Task<AgencyNewsManageDetailDto> PublishAsync(Guid uuid, CancellationToken cancellationToken = default);

    Task<AgencyNewsManageDetailDto> UnpublishAsync(Guid uuid, CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid uuid, CancellationToken cancellationToken = default);
}

public record AgencyNewsListItemDto(
    Guid Uuid,
    string Title,
    string? Summary,
    string? ImageUrl,
    string Category,
    bool IsRegulatory,
    string AgencyCode,
    string AgencyName,
    DateTime PublishedAt);

public record AgencyNewsDetailDto(
    Guid Uuid,
    string Title,
    string? Summary,
    string Body,
    string? ImageUrl,
    string Category,
    bool IsRegulatory,
    string AgencyCode,
    string AgencyName,
    DateTime PublishedAt);

public record AgencyNewsManageListItemDto(
    Guid Uuid,
    string Title,
    string Category,
    bool IsRegulatory,
    bool IsPublished,
    DateTime? PublishedAt,
    int DisplayOrder,
    DateTime UpdatedAt);

public record AgencyNewsManageDetailDto(
    Guid Uuid,
    string Title,
    string? Summary,
    string Body,
    string? ImageUrl,
    string Category,
    bool IsRegulatory,
    bool IsPublished,
    DateTime? PublishedAt,
    int DisplayOrder,
    string AgencyCode,
    string AgencyName,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public record CreateAgencyNewsRequest(
    string Title,
    string? Summary,
    string Body,
    string? ImageUrl,
    string Category,
    bool IsRegulatory,
    int? DisplayOrder);

public record UpdateAgencyNewsRequest(
    string Title,
    string? Summary,
    string Body,
    string? ImageUrl,
    string Category,
    bool IsRegulatory,
    int? DisplayOrder);
