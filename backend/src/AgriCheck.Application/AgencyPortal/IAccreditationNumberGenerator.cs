using AgriCheck.Domain.Entities;

namespace AgriCheck.Application.AgencyPortal;

public interface IAccreditationNumberGenerator
{
    Task<string> GenerateAsync(AccreditationSubmission submission, CancellationToken cancellationToken = default);
}
