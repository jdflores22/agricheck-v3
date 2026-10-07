namespace AgriCheck.Application.Compliance;

public interface IEntryDestinationValidationService
{
    Task<EntryDestinationValidationResultDto> ValidateForCurrentUserAsync(
        EntryDestinationValidationRequest request,
        CancellationToken cancellationToken = default);

    Task<EntryDestinationValidationResultDto> ValidateForImporterAsync(
        long importerUserId,
        EntryDestinationValidationRequest request,
        CancellationToken cancellationToken = default);

    Task RequireValidForEntryAsync(
        long importerUserId,
        long entryId,
        long warehouseFacilityId,
        decimal shipmentKg,
        string? formDataJson,
        CancellationToken cancellationToken = default);
}
