using AgriCheck.Application.Common;
using AgriCheck.Infrastructure.Persistence;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MimeKit;

namespace AgriCheck.Infrastructure.Email;

public class SmtpEmailService : IEmailService
{
    private readonly AgriCheckDbContext _db;
    private readonly ILogger<SmtpEmailService> _logger;

    public SmtpEmailService(AgriCheckDbContext db, ILogger<SmtpEmailService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<bool> IsEnabledAsync(CancellationToken cancellationToken = default)
    {
        var settings = await LoadSettingsAsync(cancellationToken);
        return settings.Enabled && !string.IsNullOrWhiteSpace(settings.Host);
    }

    public async Task SendAsync(string toEmail, string subject, string htmlBody, CancellationToken cancellationToken = default)
    {
        var settings = await LoadSettingsAsync(cancellationToken);
        if (!settings.Enabled || string.IsNullOrWhiteSpace(settings.Host))
        {
            _logger.LogInformation("Email skipped (disabled or no SMTP host): {Subject} -> {Email}", subject, toEmail);
            return;
        }

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(settings.FromName, settings.FromEmail));
        message.To.Add(MailboxAddress.Parse(toEmail));
        message.Subject = subject;
        message.Body = new TextPart("html") { Text = htmlBody };

        using var client = new SmtpClient();
        await client.ConnectAsync(settings.Host, settings.Port, settings.UseSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.None, cancellationToken);

        if (!string.IsNullOrWhiteSpace(settings.Username))
        {
            await client.AuthenticateAsync(settings.Username, settings.Password, cancellationToken);
        }

        await client.SendAsync(message, cancellationToken);
        await client.DisconnectAsync(true, cancellationToken);
        _logger.LogInformation("Email sent: {Subject} -> {Email}", subject, toEmail);
    }

    private async Task<SmtpSettings> LoadSettingsAsync(CancellationToken cancellationToken)
    {
        var rows = await _db.SystemSettings.AsNoTracking().ToDictionaryAsync(s => s.SettingKey, s => s.SettingValue, StringComparer.OrdinalIgnoreCase, cancellationToken);
        rows.TryGetValue("enable_email_notifications", out var enabledRaw);
        rows.TryGetValue("smtp_host", out var host);
        rows.TryGetValue("smtp_port", out var portRaw);
        rows.TryGetValue("smtp_encryption", out var encryption);
        rows.TryGetValue("smtp_username", out var username);
        rows.TryGetValue("smtp_password", out var password);
        rows.TryGetValue("from_email", out var fromEmail);
        rows.TryGetValue("from_name", out var fromName);

        var enabled = enabledRaw is "1" or "true" or "yes";
        var port = int.TryParse(portRaw, out var p) ? p : 587;
        var useSsl = string.Equals(encryption, "tls", StringComparison.OrdinalIgnoreCase)
            || string.Equals(encryption, "ssl", StringComparison.OrdinalIgnoreCase);

        return new SmtpSettings(
            enabled,
            host ?? string.Empty,
            port,
            useSsl,
            username ?? string.Empty,
            password ?? string.Empty,
            string.IsNullOrWhiteSpace(fromEmail) ? "noreply@agricheck.local" : fromEmail,
            string.IsNullOrWhiteSpace(fromName) ? "AgriCheck" : fromName);
    }

    private sealed record SmtpSettings(
        bool Enabled,
        string Host,
        int Port,
        bool UseSsl,
        string Username,
        string Password,
        string FromEmail,
        string FromName);
}
