namespace AgriCheck.Application.Common;

public interface IEmailService
{
    Task<bool> IsEnabledAsync(CancellationToken cancellationToken = default);
    Task SendAsync(string toEmail, string subject, string htmlBody, CancellationToken cancellationToken = default);
}
