using System.Text.Json;
using AgriCheck.Application.AgencyPortal;
using AgriCheck.Domain.Entities;
using AgriCheck.Domain.Enums;
using AgriCheck.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AgriCheck.Infrastructure.Services;

public class AccreditationNumberGenerator : IAccreditationNumberGenerator
{
    private readonly AgriCheckDbContext _db;

    public AccreditationNumberGenerator(AgriCheckDbContext db) => _db = db;

    public async Task<string> GenerateAsync(AccreditationSubmission submission, CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrWhiteSpace(submission.AccreditationNumber))
        {
            return submission.AccreditationNumber.Trim();
        }

        var year = DateTime.UtcNow.Year;
        var submissionPrefix = submission.SubmissionType.StartsWith("RENEWAL", StringComparison.OrdinalIgnoreCase) ? "R" : "N";
        var accreditationType = ResolveAccreditationTypeCode(submission);

        var count = await _db.AccreditationHistories
            .CountAsync(
                h => h.Status == AccreditationSubmissionStatus.Approved && h.CreatedAt.Year == year,
                cancellationToken);

        for (var attempt = 0; attempt < 1000; attempt++)
        {
            var sequence = (count + 1 + attempt).ToString("D5");
            var candidate = $"DA-{year}-{submissionPrefix}{accreditationType}-{sequence}";

            if (!await _db.AccreditationSubmissions.AnyAsync(s => s.AccreditationNumber == candidate, cancellationToken))
            {
                return candidate;
            }
        }

        throw new InvalidOperationException("Unable to generate a unique accreditation number.");
    }

    internal static string ResolveAccreditationTypeCode(AccreditationSubmission submission)
    {
        if (string.IsNullOrWhiteSpace(submission.FormDataJson))
        {
            return "GEN";
        }

        try
        {
            using var document = JsonDocument.Parse(submission.FormDataJson);
            if (!document.RootElement.TryGetProperty("business_type", out var businessType))
            {
                return "GEN";
            }

            var value = businessType.GetString()?.Trim().ToUpperInvariant();
            return value switch
            {
                "IMPORTER" => "IMP",
                "EXPORTER" => "EXP",
                "BROKER" => "BRO",
                "GENERAL" => "GEN",
                _ when !string.IsNullOrWhiteSpace(value) && value.Length >= 3 => value[..3],
                _ => "GEN",
            };
        }
        catch (JsonException)
        {
            return "GEN";
        }
    }
}
