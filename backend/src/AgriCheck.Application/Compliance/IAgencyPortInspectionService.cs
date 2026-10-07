namespace AgriCheck.Application.Compliance;

public interface IAgencyPortInspectionService
{
    Task<PortDabiInspectionDto> RecordPortDabiAsync(Guid entryUuid, RecordPortDabiInspectionRequest request, CancellationToken cancellationToken = default);
}

public record RecordPortDabiInspectionRequest(
    string? Findings,
    bool StampSpsUsed,
    DateTime? CompletedAt);

public record PortDabiInspectionDto(
    Guid InspectionUuid,
    Guid EntryUuid,
    string Status,
    DateTime? SpsUsedStampAt,
    string? ClearanceNumber);
