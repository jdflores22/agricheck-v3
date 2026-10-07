using AgriCheck.Domain.Entities;

namespace AgriCheck.Application.Compliance;

public interface IContainerImportComplianceGate
{
    Task ValidateCswReceiveAsync(Entry entry, Container container, long warehouseFacilityId, CancellationToken cancellationToken = default);
}
