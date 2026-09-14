using AgriCheck.Application.ClientPortal;
using AgriCheck.Application.ClientPortal.Dtos;
using AgriCheck.Application.Common;
using AgriCheck.Application.Notifications;
using AgriCheck.Application.Payments;
using AgriCheck.Domain.Entities;
using AgriCheck.Domain.Enums;
using AgriCheck.Infrastructure.Helpers;
using AgriCheck.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Text.Json;

namespace AgriCheck.Infrastructure.Services;

public class AccreditationService : IAccreditationService
{
    private readonly AgriCheckDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IFileStorageService _fileStorage;
    private readonly INotificationService _notifications;

    public AccreditationService(
        AgriCheckDbContext db,
        ICurrentUserService currentUser,
        IFileStorageService fileStorage,
        INotificationService notifications)
    {
        _db = db;
        _currentUser = currentUser;
        _fileStorage = fileStorage;
        _notifications = notifications;
    }

    public async Task<PagedResult<AccreditationListItemDto>> ListAsync(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var user = await UserContextHelper.RequireUserAsync(_db, _currentUser, cancellationToken);
        var query = _db.AccreditationSubmissions.Where(s => s.UserId == user.Id);
        var total = await query.CountAsync(cancellationToken);
        var rows = await query
            .Include(s => s.History)
            .OrderByDescending(s => s.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
        var items = rows
            .Select(s => new AccreditationListItemDto(
                s.Uuid,
                s.CompanyName,
                s.SubmissionType,
                s.Status.ToString(),
                AccreditationSubmissionStatusMapper.GetDisplayStatus(s),
                s.SubmittedAt,
                s.CreatedAt))
            .ToList();
        return new PagedResult<AccreditationListItemDto>(items, page, pageSize, total);
    }

    public async Task<AccreditationSubmissionDto?> GetAsync(Guid uuid, CancellationToken cancellationToken = default)
    {
        var user = await UserContextHelper.RequireUserAsync(_db, _currentUser, cancellationToken);
        var submission = await _db.AccreditationSubmissions
            .Include(s => s.Files).ThenInclude(f => f.Reviews)
            .Include(s => s.Files).ThenInclude(f => f.Versions)
            .Include(s => s.History).ThenInclude(h => h.Actor!).ThenInclude(a => a.Profile)
            .FirstOrDefaultAsync(s => s.Uuid == uuid && s.UserId == user.Id, cancellationToken);
        if (submission is null) return null;

        var certificate = await AccreditationCertificateLookup.FindForSubmissionAsync(
            _db,
            submission.Uuid,
            user.Id,
            cancellationToken);

        return Map(submission, certificate);
    }

    public async Task<AccreditationSubmissionDto> CreateAsync(CreateAccreditationRequest request, CancellationToken cancellationToken = default)
    {
        var user = await UserContextHelper.RequireUserAsync(_db, _currentUser, cancellationToken);
        var existing = await _db.AccreditationSubmissions
            .Where(s => s.UserId == user.Id)
            .OrderByDescending(s => s.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (existing is not null)
        {
            if (existing.Status == AccreditationSubmissionStatus.Draft)
            {
                throw new ClientPortalException(
                    "DRAFT_EXISTS",
                    "You already have a draft accreditation application. Continue and submit that application instead of creating a new one.");
            }

            throw new ClientPortalException(
                "SUBMISSION_EXISTS",
                "You already have an accreditation application on record.");
        }

        var submission = new AccreditationSubmission
        {
            Uuid = Guid.NewGuid(),
            UserId = user.Id,
            CompanyName = request.CompanyName.Trim(),
            SubmissionType = request.SubmissionType,
            FormDataJson = request.FormDataJson,
            Status = AccreditationSubmissionStatus.Draft
        };
        _db.AccreditationSubmissions.Add(submission);
        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            var raced = await _db.AccreditationSubmissions
                .Where(s => s.UserId == user.Id)
                .OrderByDescending(s => s.CreatedAt)
                .FirstOrDefaultAsync(cancellationToken);

            if (raced?.Status == AccreditationSubmissionStatus.Draft)
            {
                throw new ClientPortalException(
                    "DRAFT_EXISTS",
                    "You already have a draft accreditation application. Continue and submit that application instead of creating a new one.");
            }

            throw new ClientPortalException(
                "SUBMISSION_EXISTS",
                "You already have an accreditation application on record.");
        }

        return Map(await _db.AccreditationSubmissions.Include(s => s.Files).Include(s => s.History).FirstAsync(s => s.Id == submission.Id, cancellationToken));
    }

    public async Task<AccreditationSubmissionDto> UpdateAsync(Guid uuid, UpdateAccreditationRequest request, CancellationToken cancellationToken = default)
    {
        var user = await UserContextHelper.RequireUserAsync(_db, _currentUser, cancellationToken);
        var submission = await _db.AccreditationSubmissions.FirstOrDefaultAsync(s => s.Uuid == uuid && s.UserId == user.Id, cancellationToken)
            ?? throw new ClientPortalException("NOT_FOUND", "Submission not found.");
        if (submission.Status != AccreditationSubmissionStatus.Draft)
            throw new ClientPortalException("NOT_EDITABLE", "Only draft submissions can be edited.");
        submission.CompanyName = request.CompanyName.Trim();
        submission.FormDataJson = request.FormDataJson;
        await _db.SaveChangesAsync(cancellationToken);
        return Map(await _db.AccreditationSubmissions.Include(s => s.Files).Include(s => s.History).FirstAsync(s => s.Id == submission.Id, cancellationToken));
    }

    public async Task<AccreditationSubmissionDto> SubmitAsync(Guid uuid, CancellationToken cancellationToken = default)
    {
        var user = await UserContextHelper.RequireUserAsync(_db, _currentUser, cancellationToken);
        var submission = await _db.AccreditationSubmissions.FirstOrDefaultAsync(s => s.Uuid == uuid && s.UserId == user.Id, cancellationToken)
            ?? throw new ClientPortalException("NOT_FOUND", "Submission not found.");
        if (submission.Status != AccreditationSubmissionStatus.Draft)
            throw new ClientPortalException("ALREADY_SUBMITTED", "Submission already submitted.");
        submission.Status = AccreditationSubmissionStatus.Submitted;
        submission.SubmittedAt = DateTime.UtcNow;
        submission.History.Add(new AccreditationHistory { Status = AccreditationSubmissionStatus.Submitted, Comment = "Submitted by applicant", ActorUserId = user.Id });
        await _db.SaveChangesAsync(cancellationToken);
        return Map(await _db.AccreditationSubmissions.Include(s => s.Files).Include(s => s.History).FirstAsync(s => s.Id == submission.Id, cancellationToken));
    }

    public async Task<SubmissionFileDto> UploadFileAsync(Guid submissionUuid, Stream fileStream, string fileName, string contentType, CancellationToken cancellationToken = default)
    {
        var user = await UserContextHelper.RequireUserAsync(_db, _currentUser, cancellationToken);
        var submission = await _db.AccreditationSubmissions.FirstOrDefaultAsync(s => s.Uuid == submissionUuid && s.UserId == user.Id, cancellationToken)
            ?? throw new ClientPortalException("NOT_FOUND", "Submission not found.");
        if (submission.Status != AccreditationSubmissionStatus.Draft)
            throw new ClientPortalException("NOT_EDITABLE", "Files can only be uploaded to draft submissions.");

        var (storedFileName, size) = await _fileStorage.SaveAsync(fileStream, $"accreditation/{submission.Uuid}", fileName, cancellationToken);
        var file = new SubmissionFile
        {
            Uuid = Guid.NewGuid(),
            SubmissionId = submission.Id,
            OriginalFileName = fileName,
            StoredFileName = storedFileName,
            ContentType = contentType,
            FileSizeBytes = size
        };
        _db.SubmissionFiles.Add(file);
        await _db.SaveChangesAsync(cancellationToken);

        _db.SubmissionFileVersions.Add(new SubmissionFileVersion
        {
            Uuid = Guid.NewGuid(),
            SubmissionFileId = file.Id,
            VersionNumber = 1,
            OriginalFileName = fileName,
            StoredFileName = storedFileName,
            ContentType = contentType,
            FileSizeBytes = size,
            UploadedByUserId = user.Id
        });
        await _db.SaveChangesAsync(cancellationToken);

        file = await _db.SubmissionFiles.Include(f => f.Reviews).Include(f => f.Versions).FirstAsync(f => f.Id == file.Id, cancellationToken);
        return MapFile(file);
    }

    public async Task<SubmissionFileDto> UploadComplianceFileAsync(Guid submissionUuid, Guid fileUuid, Stream fileStream, string fileName, string contentType, CancellationToken cancellationToken = default)
    {
        var user = await UserContextHelper.RequireUserAsync(_db, _currentUser, cancellationToken);
        var file = await _db.SubmissionFiles
            .Include(f => f.Submission)
            .Include(f => f.Reviews)
            .Include(f => f.Versions)
            .FirstOrDefaultAsync(f => f.Uuid == fileUuid && f.Submission.Uuid == submissionUuid && f.Submission.UserId == user.Id, cancellationToken)
            ?? throw new ClientPortalException("FILE_NOT_FOUND", "File not found.");

        if (file.Submission.Status != AccreditationSubmissionStatus.RevisionRequired)
            throw new ClientPortalException("NOT_EDITABLE", "Compliance uploads are only allowed when revision is required.");

        var latestReview = file.Reviews.OrderByDescending(r => r.UpdatedAt).FirstOrDefault();
        if (latestReview?.Decision != EvaluationDecision.RevisionRequired)
            throw new ClientPortalException("FILE_NOT_EDITABLE", "This file does not require a compliance revision.");

        await ArchiveSubmissionFileVersionAsync(file, user.Id, cancellationToken);
        var (storedFileName, size) = await _fileStorage.SaveAsync(fileStream, $"accreditation/{submissionUuid}/compliance", fileName, cancellationToken);
        file.StoredFileName = storedFileName;
        file.OriginalFileName = fileName;
        file.ContentType = contentType;
        file.FileSizeBytes = size;

        var nextVersion = await NextSubmissionFileVersionNumberAsync(file.Id, cancellationToken);
        _db.SubmissionFileVersions.Add(new SubmissionFileVersion
        {
            Uuid = Guid.NewGuid(),
            SubmissionFileId = file.Id,
            VersionNumber = nextVersion,
            OriginalFileName = fileName,
            StoredFileName = storedFileName,
            ContentType = contentType,
            FileSizeBytes = size,
            UploadedByUserId = user.Id
        });

        foreach (var review in file.Reviews.Where(r => r.Decision == EvaluationDecision.RevisionRequired))
        {
            review.Decision = EvaluationDecision.Pending;
            review.Comment = null;
        }

        SyncFormDataFileName(file.Submission, file.Uuid, fileName);

        await _db.SaveChangesAsync(cancellationToken);
        file = await _db.SubmissionFiles.Include(f => f.Reviews).Include(f => f.Versions).FirstAsync(f => f.Id == file.Id, cancellationToken);
        return MapFile(file);
    }

    public async Task<AccreditationSubmissionDto> ResubmitComplianceAsync(Guid submissionUuid, CancellationToken cancellationToken = default)
    {
        var user = await UserContextHelper.RequireUserAsync(_db, _currentUser, cancellationToken);
        var submission = await _db.AccreditationSubmissions
            .Include(s => s.Files).ThenInclude(f => f.Reviews)
            .Include(s => s.Files).ThenInclude(f => f.Versions)
            .Include(s => s.History)
            .FirstOrDefaultAsync(s => s.Uuid == submissionUuid && s.UserId == user.Id, cancellationToken)
            ?? throw new ClientPortalException("NOT_FOUND", "Submission not found.");

        if (submission.Status != AccreditationSubmissionStatus.RevisionRequired)
            throw new ClientPortalException("INVALID_STATUS", "Only submissions marked for revision can be resubmitted.");

        var pendingRevisions = submission.Files
            .SelectMany(f => f.Reviews)
            .Any(r => r.Decision == EvaluationDecision.RevisionRequired);
        if (pendingRevisions)
            throw new ClientPortalException("COMPLIANCE_INCOMPLETE", "Upload revised documents for all flagged files before resubmitting.");

        submission.Status = AccreditationSubmissionStatus.UnderReview;
        submission.History.Add(new AccreditationHistory
        {
            Status = AccreditationSubmissionStatus.UnderReview,
            Comment = "Revised documents resubmitted by applicant",
            ActorUserId = user.Id
        });
        await _db.SaveChangesAsync(cancellationToken);

        if (submission.AssignedOfficerUserId is not null)
        {
            await _notifications.NotifyAsync(
                submission.AssignedOfficerUserId.Value,
                "accreditation_resubmitted",
                "Accreditation resubmitted",
                $"{submission.CompanyName} resubmitted revised accreditation documents for your review.",
                "AccreditationSubmission",
                submission.Uuid.ToString(),
                cancellationToken);
        }

        await _notifications.NotifyAsync(
            user.Id,
            "accreditation_resubmitted",
            "Accreditation resubmitted",
            $"Revised accreditation documents for {submission.CompanyName} were resubmitted.",
            "AccreditationSubmission",
            submission.Uuid.ToString(),
            cancellationToken);

        return Map(submission);
    }

    public async Task<StoredFileDownload> DownloadFileAsync(Guid submissionUuid, Guid fileUuid, CancellationToken cancellationToken = default)
    {
        var user = await UserContextHelper.RequireUserAsync(_db, _currentUser, cancellationToken);
        var file = await _db.SubmissionFiles
            .Include(f => f.Submission)
            .FirstOrDefaultAsync(f => f.Uuid == fileUuid && f.Submission.Uuid == submissionUuid && f.Submission.UserId == user.Id, cancellationToken)
            ?? throw new ClientPortalException("FILE_NOT_FOUND", "File not found.");

        return new StoredFileDownload(
            StoredFilePathResolver.ResolveAccreditationFilePath(_fileStorage, submissionUuid, file.StoredFileName),
            file.ContentType,
            file.OriginalFileName);
    }

    public async Task<StoredFileDownload> DownloadFileVersionAsync(
        Guid submissionUuid,
        Guid fileUuid,
        int versionNumber,
        CancellationToken cancellationToken = default)
    {
        var user = await UserContextHelper.RequireUserAsync(_db, _currentUser, cancellationToken);
        var file = await _db.SubmissionFiles
            .Include(f => f.Submission)
            .Include(f => f.Versions)
            .FirstOrDefaultAsync(
                f => f.Uuid == fileUuid && f.Submission.Uuid == submissionUuid && f.Submission.UserId == user.Id,
                cancellationToken)
            ?? throw new ClientPortalException("FILE_NOT_FOUND", "File not found.");

        var version = file.Versions.FirstOrDefault(v => v.VersionNumber == versionNumber)
            ?? throw new ClientPortalException("VERSION_NOT_FOUND", "Document version not found.");

        return new StoredFileDownload(
            StoredFilePathResolver.ResolveAccreditationFilePath(_fileStorage, submissionUuid, version.StoredFileName),
            version.ContentType,
            version.OriginalFileName);
    }

    private static AccreditationSubmissionDto Map(AccreditationSubmission s, Certificate? certificate = null) => new(
        s.Uuid, s.CompanyName, s.SubmissionType, s.Status.ToString(), AccreditationSubmissionStatusMapper.GetDisplayStatus(s), s.FormDataJson, s.ReviewComments, s.AccreditationNumber, s.SubmittedAt,
        certificate?.Uuid,
        certificate?.CertificateNumber,
        s.Files.Select(MapFile).ToList(),
        s.History.OrderByDescending(h => h.CreatedAt).Select(h => AccreditationHistoryMapper.ForClient(h, s.UserId)).ToList());

    private static SubmissionFileDto MapFile(SubmissionFile file)
    {
        var review = file.Reviews.OrderByDescending(r => r.UpdatedAt).FirstOrDefault();
        var versions = file.Versions
            .OrderByDescending(v => v.VersionNumber)
            .Select(v => new SubmissionFileVersionDto(
                v.VersionNumber,
                v.OriginalFileName,
                v.FileSizeBytes,
                v.CreatedAt,
                v.StoredFileName == file.StoredFileName))
            .ToList();
        return new SubmissionFileDto(
            file.Uuid,
            file.OriginalFileName,
            file.FileSizeBytes,
            file.CreatedAt,
            review?.Decision.ToString(),
            review?.Comment,
            versions);
    }

    private static void SyncFormDataFileName(AccreditationSubmission submission, Guid fileUuid, string fileName)
    {
        if (string.IsNullOrWhiteSpace(submission.FormDataJson))
        {
            return;
        }

        try
        {
            var values = JsonSerializer.Deserialize<Dictionary<string, string>>(submission.FormDataJson);
            if (values is null)
            {
                return;
            }

            var uuidKey = values.FirstOrDefault(entry =>
                entry.Key.EndsWith("_file_uuid", StringComparison.Ordinal) &&
                string.Equals(entry.Value, fileUuid.ToString(), StringComparison.OrdinalIgnoreCase)).Key;

            if (uuidKey is null)
            {
                return;
            }

            var fieldName = uuidKey[..^"_file_uuid".Length];
            values[fieldName] = fileName;
            submission.FormDataJson = JsonSerializer.Serialize(values);
        }
        catch (JsonException)
        {
            // Keep legacy/non-JSON form payloads unchanged.
        }
    }

    private async Task<int> NextSubmissionFileVersionNumberAsync(long submissionFileId, CancellationToken cancellationToken)
    {
        var max = await _db.SubmissionFileVersions.Where(v => v.SubmissionFileId == submissionFileId).MaxAsync(v => (int?)v.VersionNumber, cancellationToken);
        return (max ?? 0) + 1;
    }

    private async Task ArchiveSubmissionFileVersionAsync(SubmissionFile file, long userId, CancellationToken cancellationToken)
    {
        if (await _db.SubmissionFileVersions.AnyAsync(v => v.SubmissionFileId == file.Id && v.StoredFileName == file.StoredFileName, cancellationToken))
            return;

        var versionNumber = await NextSubmissionFileVersionNumberAsync(file.Id, cancellationToken);
        _db.SubmissionFileVersions.Add(new SubmissionFileVersion
        {
            Uuid = Guid.NewGuid(),
            SubmissionFileId = file.Id,
            VersionNumber = versionNumber,
            OriginalFileName = file.OriginalFileName,
            StoredFileName = file.StoredFileName,
            ContentType = file.ContentType,
            FileSizeBytes = file.FileSizeBytes,
            UploadedByUserId = userId
        });
    }
}

public class CertificateService : ICertificateService
{
    private readonly AgriCheckDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IFileStorageService _fileStorage;

    public CertificateService(AgriCheckDbContext db, ICurrentUserService currentUser, IFileStorageService fileStorage)
    {
        _db = db;
        _currentUser = currentUser;
        _fileStorage = fileStorage;
    }

    public async Task<PagedResult<CertificateListItemDto>> ListAsync(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var user = await UserContextHelper.RequireUserAsync(_db, _currentUser, cancellationToken);
        var query = _db.Certificates.Include(c => c.Entry).Where(c => c.UserId == user.Id && c.Status == CertificateStatus.Active);
        var total = await query.CountAsync(cancellationToken);
        var items = await query.OrderByDescending(c => c.IssuedAt).Skip((page - 1) * pageSize).Take(pageSize)
            .Select(c => new CertificateListItemDto(c.Uuid, c.CertificateNumber, c.Title, c.Status.ToString(), c.IssuedAt, c.ExpiresAt, c.Entry != null ? c.Entry.ReferenceNo : null))
            .ToListAsync(cancellationToken);
        return new PagedResult<CertificateListItemDto>(items, page, pageSize, total);
    }

    public async Task<CertificateDto?> GetAsync(Guid uuid, CancellationToken cancellationToken = default)
    {
        var user = await UserContextHelper.RequireUserAsync(_db, _currentUser, cancellationToken);
        var cert = await _db.Certificates.Include(c => c.Entry).FirstOrDefaultAsync(c => c.Uuid == uuid && c.UserId == user.Id, cancellationToken);
        return cert is null ? null : Map(cert);
    }

    public async Task<StoredFileDownload?> GetPdfAsync(Guid uuid, CancellationToken cancellationToken = default)
    {
        var user = await UserContextHelper.RequireUserAsync(_db, _currentUser, cancellationToken);
        var cert = await _db.Certificates.FirstOrDefaultAsync(c => c.Uuid == uuid && c.UserId == user.Id, cancellationToken);
        if (cert is null || cert.Status != CertificateStatus.Active || string.IsNullOrWhiteSpace(cert.PdfStoredFileName))
        {
            return null;
        }

        var physicalPath = _fileStorage.GetPhysicalPath($"certificates/{cert.Uuid}", cert.PdfStoredFileName);
        if (!File.Exists(physicalPath))
        {
            return null;
        }

        return new StoredFileDownload(
            physicalPath,
            "application/pdf",
            $"certificate-{cert.CertificateNumber}.pdf");
    }

    public async Task<CertificateVerifyDto?> VerifyAsync(string verificationCode, CancellationToken cancellationToken = default)
    {
        var cert = await _db.Certificates.Include(c => c.User).ThenInclude(u => u.Profile)
            .FirstOrDefaultAsync(c => c.VerificationCode == verificationCode, cancellationToken);
        if (cert is null) return null;

        var effectiveStatus = ResolveEffectiveCertificateStatus(cert);
        var isValid = effectiveStatus == "ACTIVE";
        var holder = cert.User.Profile is null
            ? cert.User.Email
            : $"{cert.User.Profile.FirstName} {cert.User.Profile.LastName}".Trim();
        if (string.IsNullOrWhiteSpace(holder))
        {
            holder = cert.User.Email;
        }

        var summary = ParseCertificateSummary(cert.SummaryJson);
        var companyName = summary.CompanyName
            ?? cert.User.Profile?.CompanyName
            ?? holder;

        return new CertificateVerifyDto(
            cert.CertificateNumber,
            cert.Title,
            cert.Status.ToString(),
            effectiveStatus,
            cert.IssuedAt,
            cert.ExpiresAt,
            isValid,
            holder,
            summary.ProcessType,
            companyName,
            summary.CompanyType,
            summary.AccreditationNumber,
            cert.QrCodeData,
            cert.RevokedAt,
            cert.RevokedReason);
    }

    private static string ResolveEffectiveCertificateStatus(Certificate cert)
    {
        if (cert.Status == CertificateStatus.Revoked || cert.RevokedAt.HasValue)
        {
            return "REVOKED";
        }

        if (cert.Status == CertificateStatus.Expired ||
            (cert.ExpiresAt.HasValue && cert.ExpiresAt <= DateTime.UtcNow))
        {
            return "EXPIRED";
        }

        return "ACTIVE";
    }

    private static (string? ProcessType, string? CompanyName, string? CompanyType, string? AccreditationNumber)
        ParseCertificateSummary(string? summaryJson)
    {
        if (string.IsNullOrWhiteSpace(summaryJson))
        {
            return (null, null, null, null);
        }

        try
        {
            using var document = JsonDocument.Parse(summaryJson);
            var root = document.RootElement;
            var processType = root.TryGetProperty("processType", out var processTypeElement)
                ? processTypeElement.GetString()
                : null;
            var companyName = root.TryGetProperty("companyName", out var companyNameElement)
                ? companyNameElement.GetString()
                : null;
            var accreditationNumber = root.TryGetProperty("accreditationNumber", out var accreditationNumberElement)
                ? accreditationNumberElement.GetString()
                : null;
            return (processType, companyName, null, accreditationNumber);
        }
        catch (JsonException)
        {
            return (null, null, null, null);
        }
    }

    private static CertificateDto Map(Certificate c) => new(
        c.Uuid, c.CertificateNumber, c.VerificationCode, c.Title, c.Status.ToString(), c.IssuedAt, c.ExpiresAt,
        c.Entry?.ReferenceNo, c.QrCodeData, c.SummaryJson);
}

public class WarehouseBookingService : IWarehouseBookingService
{
    private readonly AgriCheckDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public WarehouseBookingService(AgriCheckDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<WarehouseFacilityDto>> ListFacilitiesAsync(CancellationToken cancellationToken = default)
    {
        var facilities = await _db.WarehouseFacilities.AsNoTracking()
            .Where(f => f.IsActive)
            .Include(f => f.Region)
            .Include(f => f.Province)
            .Include(f => f.City)
            .Include(f => f.Barangay)
            .OrderBy(f => f.Name)
            .ToListAsync(cancellationToken);

        return facilities.Select(f => new WarehouseFacilityDto(
            f.Id,
            f.Code,
            f.Name,
            f.Location,
            f.Capacity,
            WarehouseAddressFormatter.Format(
                f.StreetAddress,
                f.Barangay?.Name,
                f.City?.Name,
                f.Province?.Name,
                f.Region?.Name,
                f.ZipCode ?? f.Barangay?.ZipCode,
                f.Location),
            f.Latitude,
            f.Longitude,
            f.Region?.Name,
            f.Province?.Name,
            f.City?.Name,
            f.Barangay?.Name,
            f.StreetAddress,
            f.ZipCode ?? f.Barangay?.ZipCode)).ToList();
    }

    public async Task<PagedResult<WarehouseBookingListItemDto>> ListBookingsAsync(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var user = await UserContextHelper.RequireUserAsync(_db, _currentUser, cancellationToken);
        var query = _db.WarehouseBookings.Include(b => b.WarehouseFacility).Where(b => b.UserId == user.Id);
        var total = await query.CountAsync(cancellationToken);
        var items = await query.OrderByDescending(b => b.CreatedAt).Skip((page - 1) * pageSize).Take(pageSize)
            .Select(b => new WarehouseBookingListItemDto(b.Uuid, b.BookingNumber, b.WarehouseFacility.Name, b.ContainerReference, b.Status.ToString(), b.ScheduledDate, b.Amount))
            .ToListAsync(cancellationToken);
        return new PagedResult<WarehouseBookingListItemDto>(items, page, pageSize, total);
    }

    public async Task<WarehouseBookingDto?> GetBookingAsync(Guid uuid, CancellationToken cancellationToken = default)
    {
        var user = await UserContextHelper.RequireUserAsync(_db, _currentUser, cancellationToken);
        var booking = await _db.WarehouseBookings.Include(b => b.WarehouseFacility)
            .FirstOrDefaultAsync(b => b.Uuid == uuid && b.UserId == user.Id, cancellationToken);
        return booking is null ? null : Map(booking);
    }

    public async Task<WarehouseBookingDto> CreateBookingAsync(CreateWarehouseBookingRequest request, CancellationToken cancellationToken = default)
    {
        var user = await UserContextHelper.RequireUserAsync(_db, _currentUser, cancellationToken);
        var facility = await _db.WarehouseFacilities.FirstOrDefaultAsync(f => f.Id == request.WarehouseFacilityId && f.IsActive, cancellationToken)
            ?? throw new ClientPortalException("FACILITY_NOT_FOUND", "Warehouse facility not found.");

        Container? linkedContainer = null;
        if (request.ContainerUuid is Guid containerUuid)
        {
            linkedContainer = await _db.Containers
                .Include(c => c.Entry)
                .FirstOrDefaultAsync(c => c.Uuid == containerUuid && c.Entry.UserId == user.Id, cancellationToken)
                ?? throw new ClientPortalException("CONTAINER_NOT_FOUND", "Container not found.");
        }
        else
        {
            var reference = request.ContainerReference.Trim();
            linkedContainer = await _db.Containers
                .Include(c => c.Entry)
                .Where(c => c.Entry.UserId == user.Id &&
                            (c.ContainerNumber == reference || c.Uuid.ToString() == reference))
                .OrderByDescending(c => c.UpdatedAt)
                .FirstOrDefaultAsync(cancellationToken);
        }

        if (linkedContainer is not null)
        {
            var (canBook, blockedReason) = ClientContainerProcessHelper.EvaluateBookingEligibility(linkedContainer, linkedContainer.Entry);
            if (!canBook)
            {
                throw new ClientPortalException(
                    "CONTAINER_NOT_PROCESSED",
                    blockedReason ?? "Warehouse booking is not available until the container is processed.");
            }
        }

        var containerReference = linkedContainer?.ContainerNumber ?? request.ContainerReference.Trim();
        var amount = 2500m;
        var booking = new WarehouseBooking
        {
            Uuid = Guid.NewGuid(),
            BookingNumber = await ReferenceNumberGenerator.BookingAsync(_db, cancellationToken),
            UserId = user.Id,
            WarehouseFacilityId = facility.Id,
            ContainerReference = containerReference,
            ScheduledDate = request.ScheduledDate,
            Notes = request.Notes,
            Amount = amount,
            Status = WarehouseBookingStatus.PaymentPending
        };
        _db.WarehouseBookings.Add(booking);
        await _db.SaveChangesAsync(cancellationToken);

        var bill = new ClientBill
        {
            Uuid = Guid.NewGuid(),
            UserId = user.Id,
            EntryId = linkedContainer?.EntryId,
            WarehouseBookingId = booking.Id,
            BillNumber = await ReferenceNumberGenerator.BillAsync(_db, cancellationToken),
            Description = $"Warehouse storage booking {booking.BookingNumber}",
            Amount = amount,
            Status = ClientBillStatus.Unpaid,
            DueDate = request.ScheduledDate.Date.AddDays(-1)
        };
        _db.ClientBills.Add(bill);
        await _db.SaveChangesAsync(cancellationToken);

        return Map(await _db.WarehouseBookings.Include(b => b.WarehouseFacility).FirstAsync(b => b.Id == booking.Id, cancellationToken));
    }

    public async Task CancelBookingAsync(Guid uuid, CancellationToken cancellationToken = default)
    {
        var user = await UserContextHelper.RequireUserAsync(_db, _currentUser, cancellationToken);
        var booking = await _db.WarehouseBookings.FirstOrDefaultAsync(b => b.Uuid == uuid && b.UserId == user.Id, cancellationToken)
            ?? throw new ClientPortalException("NOT_FOUND", "Booking not found.");
        if (booking.Status is WarehouseBookingStatus.Completed or WarehouseBookingStatus.Cancelled)
            throw new ClientPortalException("NOT_CANCELLABLE", "Booking cannot be cancelled.");
        booking.Status = WarehouseBookingStatus.Cancelled;
        await _db.SaveChangesAsync(cancellationToken);
    }

    private static WarehouseBookingDto Map(WarehouseBooking b) => new(
        b.Uuid, b.BookingNumber, b.WarehouseFacility.Name, b.ContainerReference, b.Status.ToString(), b.ScheduledDate, b.Notes, b.Amount);
}

public class ClientBillService : IClientBillService
{
    private readonly AgriCheckDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IPaymentGatewayService _paymentGateway;
    private readonly INotificationService _notifications;
    private readonly IConfiguration _configuration;
    private readonly IEntryWorkflowService _workflow;

    public ClientBillService(
        AgriCheckDbContext db,
        ICurrentUserService currentUser,
        IPaymentGatewayService paymentGateway,
        INotificationService notifications,
        IConfiguration configuration,
        IEntryWorkflowService workflow)
    {
        _db = db;
        _currentUser = currentUser;
        _paymentGateway = paymentGateway;
        _notifications = notifications;
        _configuration = configuration;
        _workflow = workflow;
    }

    public async Task<IReadOnlyList<ClientBillSummaryDto>> ListForUserAsync(CancellationToken cancellationToken = default)
    {
        var user = await UserContextHelper.RequireUserAsync(_db, _currentUser, cancellationToken);
        await AgencyBillingClientBillHelper.BackfillMissingClientBillsForUserAsync(_db, user.Id, cancellationToken);
        return await _db.ClientBills.Where(b => b.UserId == user.Id).OrderByDescending(b => b.CreatedAt)
            .Select(b => new ClientBillSummaryDto(b.Uuid, b.BillNumber, b.Description, b.Amount, b.Status.ToString(), b.DueDate, b.PaidAt))
            .ToListAsync(cancellationToken);
    }

    public async Task<ClientBillDto?> GetAsync(Guid uuid, CancellationToken cancellationToken = default)
    {
        var user = await UserContextHelper.RequireUserAsync(_db, _currentUser, cancellationToken);
        await AgencyBillingClientBillHelper.BackfillMissingClientBillsForUserAsync(_db, user.Id, cancellationToken);
        var bill = await QueryBill().FirstOrDefaultAsync(b => b.Uuid == uuid && b.UserId == user.Id, cancellationToken);
        return bill is null ? null : Map(bill);
    }

    public async Task<ClientBillDto?> GetByPaymentTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        var bill = await QueryBill().FirstOrDefaultAsync(b => b.PaymentLinkToken == token, cancellationToken);
        return bill is null ? null : Map(bill);
    }

    public async Task<ClientBillDto> PayAsync(Guid uuid, PayBillRequest request, CancellationToken cancellationToken = default)
    {
        var user = await UserContextHelper.RequireUserAsync(_db, _currentUser, cancellationToken);
        var bill = await QueryBill().FirstOrDefaultAsync(b => b.Uuid == uuid && b.UserId == user.Id, cancellationToken)
            ?? throw new ClientPortalException("NOT_FOUND", "Bill not found.");
        return await CompleteBillPaymentAsync(bill, request.PaymentMethod, $"SIM-{Guid.NewGuid():N[..12]}", null, cancellationToken);
    }

    public async Task<ClientBillDto> PayByTokenAsync(string token, PayBillRequest request, CancellationToken cancellationToken = default)
    {
        var bill = await QueryBill().FirstOrDefaultAsync(b => b.PaymentLinkToken == token, cancellationToken)
            ?? throw new ClientPortalException("NOT_FOUND", "Payment link not found.");
        return await CompleteBillPaymentAsync(bill, request.PaymentMethod, $"SIM-{Guid.NewGuid():N[..12]}", null, cancellationToken);
    }

    public async Task<BillPaymentOptionsDto> GetPaymentOptionsAsync(Guid uuid, CancellationToken cancellationToken = default)
    {
        var user = await UserContextHelper.RequireUserAsync(_db, _currentUser, cancellationToken);
        var bill = await QueryBill().FirstOrDefaultAsync(b => b.Uuid == uuid && b.UserId == user.Id, cancellationToken)
            ?? throw new ClientPortalException("NOT_FOUND", "Bill not found.");

        var agencyId = bill.Entry?.AgencyId;
        if (agencyId is null)
        {
            return new BillPaymentOptionsDto(false, true, "simulated", null);
        }

        var agencySettings = await PaymentSettingsReader.TryGetAgencySettingsAsync(_db, agencyId.Value, cancellationToken);
        var gateway = await PaymentSettingsReader.ResolveForAgencyAsync(_db, agencyId.Value, _configuration, cancellationToken);

        return new BillPaymentOptionsDto(
            gateway.PayMongoEnabled,
            gateway.CashPaymentEnabled,
            gateway.Mode,
            agencySettings?.CashPaymentInstructions);
    }

    public async Task<InitiateBillPaymentResultDto> InitiatePaymentAsync(Guid uuid, PayBillRequest request, CancellationToken cancellationToken = default)
    {
        var user = await UserContextHelper.RequireUserAsync(_db, _currentUser, cancellationToken);
        var bill = await QueryBill().FirstOrDefaultAsync(b => b.Uuid == uuid && b.UserId == user.Id, cancellationToken)
            ?? throw new ClientPortalException("NOT_FOUND", "Bill not found.");
        return await InitiateBillPaymentCoreAsync(bill, request, null, cancellationToken);
    }

    public async Task<InitiateBillPaymentResultDto> InitiatePaymentByTokenAsync(string token, PayBillRequest request, CancellationToken cancellationToken = default)
    {
        var bill = await QueryBill().FirstOrDefaultAsync(b => b.PaymentLinkToken == token, cancellationToken)
            ?? throw new ClientPortalException("NOT_FOUND", "Payment link not found.");
        return await InitiateBillPaymentCoreAsync(bill, request, token, cancellationToken);
    }

    private async Task<InitiateBillPaymentResultDto> InitiateBillPaymentCoreAsync(
        ClientBill bill,
        PayBillRequest request,
        string? paymentToken,
        CancellationToken cancellationToken)
    {
        if (bill.Status == ClientBillStatus.Paid)
        {
            throw new ClientPortalException("ALREADY_PAID", "Bill is already paid.");
        }

        var agencyId = bill.Entry?.AgencyId;
        var gateway = agencyId is long resolvedAgencyId
            ? await PaymentSettingsReader.ResolveForAgencyAsync(_db, resolvedAgencyId, _configuration, cancellationToken)
            : null;

        if (string.Equals(request.PaymentMethod, "cash", StringComparison.OrdinalIgnoreCase))
        {
            if (gateway is not null && !gateway.CashPaymentEnabled)
            {
                throw new ClientPortalException("CASH_DISABLED", "Cash payment is not enabled for this agency.");
            }

            var orNumber = request.PaymentReference?.Trim();
            if (string.IsNullOrWhiteSpace(orNumber))
            {
                throw new ClientPortalException("PAYMENT_REFERENCE_REQUIRED", "Official receipt / OR number is required for cash payment.");
            }

            ExpirePendingPayments(bill);
            bill.Status = ClientBillStatus.Pending;
            bill.Payments.Add(new ClientBillPayment
            {
                Amount = bill.Amount,
                PaymentMethod = "cash",
                ExternalReference = orNumber,
                Status = "awaiting_verification"
            });
            await _db.SaveChangesAsync(cancellationToken);

            var mapped = Map(await QueryBill().FirstAsync(b => b.Id == bill.Id, cancellationToken));
            return new InitiateBillPaymentResultDto("cash", orNumber, null, mapped);
        }

        var paymentReference = $"PAY-{DateTime.UtcNow:yyyyMMdd}-{bill.Id:D5}";
        var successUrl = BuildPaymentReturnUrl(bill.Uuid, "success", paymentToken, request.ReturnBaseUrl);
        var cancelUrl = BuildPaymentReturnUrl(bill.Uuid, "cancelled", paymentToken, request.ReturnBaseUrl);
        PaymentIntentResult intent;
        try
        {
            intent = await _paymentGateway.CreatePaymentIntentAsync(
                bill.Amount,
                bill.Description,
                paymentReference,
                request.PaymentMethod,
                successUrl,
                cancelUrl,
                agencyId,
                cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            throw new ClientPortalException("PAYMONGO_ERROR", ex.Message);
        }

        if (intent.Mode == "simulated")
        {
            var paid = await CompleteBillPaymentAsync(bill, request.PaymentMethod, paymentReference, null, cancellationToken);
            return new InitiateBillPaymentResultDto("simulated", paymentReference, null, paid);
        }

        ExpirePendingPayments(bill);
        bill.Status = ClientBillStatus.Pending;
        bill.Payments.Add(new ClientBillPayment
        {
            Amount = bill.Amount,
            PaymentMethod = request.PaymentMethod,
            ExternalReference = paymentReference,
            GatewayTransactionId = intent.GatewayTransactionId,
            PaymentUrl = intent.PaymentUrl,
            Status = "pending"
        });
        await _db.SaveChangesAsync(cancellationToken);
        return new InitiateBillPaymentResultDto(intent.Mode, paymentReference, intent.PaymentUrl, Map(await QueryBill().FirstAsync(b => b.Id == bill.Id, cancellationToken)));
    }

    public async Task<ClientBillDto> ConfirmPaymentAsync(Guid uuid, CancellationToken cancellationToken = default)
    {
        var user = await UserContextHelper.RequireUserAsync(_db, _currentUser, cancellationToken);
        var bill = await QueryBill().FirstOrDefaultAsync(b => b.Uuid == uuid && b.UserId == user.Id, cancellationToken)
            ?? throw new ClientPortalException("NOT_FOUND", "Bill not found.");

        return await ConfirmPendingBillPaymentAsync(bill, cancellationToken);
    }

    public async Task<ClientBillDto> ConfirmPaymentByTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        var bill = await QueryBill().FirstOrDefaultAsync(b => b.PaymentLinkToken == token, cancellationToken)
            ?? throw new ClientPortalException("NOT_FOUND", "Payment link not found.");

        return await ConfirmPendingBillPaymentAsync(bill, cancellationToken);
    }

    public async Task<byte[]?> GetReceiptPdfAsync(Guid billUuid, CancellationToken cancellationToken = default)
    {
        var user = await UserContextHelper.RequireUserAsync(_db, _currentUser, cancellationToken);
        var bill = await _db.ClientBills
            .Include(b => b.Payments)
            .Include(b => b.Entry)
            .Include(b => b.User).ThenInclude(u => u.Profile)
            .FirstOrDefaultAsync(b => b.Uuid == billUuid && b.UserId == user.Id, cancellationToken);
        if (bill is null || bill.Status != ClientBillStatus.Paid) return null;

        var payment = bill.Payments.FirstOrDefault(p => p.Status == "completed")
            ?? bill.Payments.OrderByDescending(p => p.CreatedAt).FirstOrDefault();
        if (payment is null) return null;

        var payer = bill.User.Profile is null
            ? bill.User.Email
            : $"{bill.User.Profile.FirstName} {bill.User.Profile.LastName}".Trim();

        return PaymentReceiptPdfGenerator.Generate(
            bill.BillNumber,
            bill.Description,
            payment.Amount,
            payment.PaymentMethod,
            payment.ExternalReference,
            bill.PaidAt ?? payment.CreatedAt,
            payer,
            bill.Entry?.ReferenceNo);
    }

    public async Task CompletePaymentByReferenceAsync(string paymentReference, string? gatewayTransactionId, CancellationToken cancellationToken = default)
    {
        var payment = await _db.ClientBillPayments
            .Include(p => p.ClientBill).ThenInclude(b => b.Entry)
            .Where(p =>
                p.ExternalReference == paymentReference ||
                p.GatewayTransactionId == paymentReference)
            .OrderByDescending(p => p.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new ClientPortalException("NOT_FOUND", "Payment not found.");

        if (payment.ClientBill.Status == ClientBillStatus.Paid) return;
        await CompleteBillPaymentAsync(
            payment.ClientBill,
            payment.PaymentMethod,
            payment.ExternalReference ?? paymentReference,
            gatewayTransactionId ?? payment.GatewayTransactionId,
            cancellationToken);
    }

    private string BuildPaymentReturnUrl(Guid billUuid, string status, string? paymentToken = null, string? returnBaseUrl = null)
    {
        var publicBase = ResolveReturnBaseUrl(returnBaseUrl);
        var url = $"{publicBase}/client/payment/return/{billUuid}?status={status}";
        if (!string.IsNullOrWhiteSpace(paymentToken))
        {
            url += $"&token={Uri.EscapeDataString(paymentToken)}";
        }

        return url;
    }

    private string ResolveReturnBaseUrl(string? requested)
    {
        var configured = (_configuration["App:PublicBaseUrl"] ?? "http://localhost:5173").TrimEnd('/');
        if (string.IsNullOrWhiteSpace(requested))
        {
            return configured;
        }

        if (!Uri.TryCreate(requested.Trim(), UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            return configured;
        }

        var origin = uri.GetLeftPart(UriPartial.Authority).TrimEnd('/');
        var allowed = _configuration.GetSection("Cors:AllowedOrigins").GetChildren()
            .Select(item => item.Value)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Cast<string>()
            .Concat(new[]
            {
                configured,
                "http://localhost:5173",
                "http://127.0.0.1:5173",
                "https://dimgrey-hummingbird-677957.hostingersite.com",
            })
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.TrimEnd('/'))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return allowed.Contains(origin) ? origin : configured;
    }

    private static void ExpirePendingPayments(ClientBill bill)
    {
        foreach (var pending in bill.Payments.Where(p => p.Status == "pending"))
        {
            pending.Status = "expired";
        }
    }

    private async Task<ClientBillDto> ConfirmPendingBillPaymentAsync(ClientBill bill, CancellationToken cancellationToken)
    {
        if (bill.Status == ClientBillStatus.Paid)
        {
            return Map(bill);
        }

        var pendingPayments = bill.Payments
            .Where(p => p.Status == "pending")
            .OrderByDescending(p => p.CreatedAt)
            .ToList();

        if (pendingPayments.Count == 0)
        {
            return Map(bill);
        }

        ClientBillPayment? paidPending = null;
        foreach (var pending in pendingPayments)
        {
            if (string.IsNullOrWhiteSpace(pending.GatewayTransactionId))
            {
                continue;
            }

            if (await _paymentGateway.IsGatewayPaymentPaidAsync(pending.GatewayTransactionId, bill.Entry?.AgencyId, cancellationToken))
            {
                paidPending = pending;
                break;
            }
        }

        if (paidPending is null)
        {
            return Map(bill);
        }

        return await CompleteBillPaymentAsync(
            bill,
            paidPending.PaymentMethod,
            paidPending.ExternalReference ?? string.Empty,
            paidPending.GatewayTransactionId,
            cancellationToken);
    }

    private async Task<ClientBillDto> CompleteBillPaymentAsync(ClientBill bill, string paymentMethod, string externalReference, string? gatewayTransactionId, CancellationToken cancellationToken)
    {
        if (bill.Status == ClientBillStatus.Paid)
            throw new ClientPortalException("ALREADY_PAID", "Bill is already paid.");

        bill.Status = ClientBillStatus.Paid;
        bill.PaidAt = DateTime.UtcNow;

        var pending = bill.Payments.FirstOrDefault(p => p.Status == "pending" && p.ExternalReference == externalReference);
        if (pending is not null)
        {
            pending.Status = "completed";
            pending.GatewayTransactionId = gatewayTransactionId ?? pending.GatewayTransactionId;
        }
        else
        {
            bill.Payments.Add(new ClientBillPayment
            {
                Amount = bill.Amount,
                PaymentMethod = paymentMethod,
                ExternalReference = externalReference,
                GatewayTransactionId = gatewayTransactionId,
                Status = "completed"
            });
        }

        Entry? paidEntry = null;
        if (bill.EntryId is not null)
        {
            paidEntry = await _db.Entries.FirstAsync(e => e.Id == bill.EntryId, cancellationToken);
            paidEntry.PaymentStatus = PaymentStatus.Paid;
        }

        if (bill.WarehouseBookingId is long warehouseBookingId)
        {
            var booking = await _db.WarehouseBookings.FirstOrDefaultAsync(b => b.Id == warehouseBookingId, cancellationToken);
            if (booking is not null && booking.Status == WarehouseBookingStatus.PaymentPending)
            {
                booking.Status = WarehouseBookingStatus.Confirmed;
            }
        }

        await _db.SaveChangesAsync(cancellationToken);

        await _notifications.NotifyAsync(
            bill.UserId,
            "payment_completed",
            "Payment received",
            $"Payment for bill {bill.BillNumber} has been completed.",
            "Bill",
            bill.Uuid.ToString(),
            cancellationToken);

        if (paidEntry is not null)
        {
            if (bill.AgencyBillingId is not null)
            {
                await AgencyBillingClientBillHelper.TryCompleteAgencyBillingFromClientBillAsync(
                    _db,
                    _workflow,
                    bill,
                    bill.UserId,
                    cancellationToken);
            }
            else
            {
                await AgencyEvaluatorNotificationHelper.NotifyEntryReadyForEvaluationAsync(
                    _db,
                    _notifications,
                    paidEntry,
                    cancellationToken);
            }
        }

        return Map(await QueryBill().FirstAsync(b => b.Id == bill.Id, cancellationToken));
    }

    public async Task<IReadOnlyList<ClientPaymentHistoryItemDto>> ListPaymentHistoryAsync(CancellationToken cancellationToken = default)
    {
        var user = await UserContextHelper.RequireUserAsync(_db, _currentUser, cancellationToken);
        return await _db.ClientBillPayments
            .Include(p => p.ClientBill).ThenInclude(b => b.Entry)
            .Where(p => p.ClientBill.UserId == user.Id)
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => new ClientPaymentHistoryItemDto(
                p.ClientBill.Uuid,
                p.ClientBill.BillNumber,
                p.ClientBill.Entry != null ? p.ClientBill.Entry.ReferenceNo : null,
                p.Amount,
                p.PaymentMethod,
                p.Status,
                p.ExternalReference,
                p.CreatedAt))
            .ToListAsync(cancellationToken);
    }

    private IQueryable<ClientBill> QueryBill() =>
        _db.ClientBills.Include(b => b.Payments).Include(b => b.Entry);

    private static ClientBillDto Map(ClientBill b) => new(
        b.Uuid, b.BillNumber, b.Description, b.Amount, b.Status.ToString(), b.DueDate, b.PaymentLinkToken,
        b.Entry?.Uuid, b.Entry?.ReferenceNo,
        b.Payments.OrderByDescending(p => p.CreatedAt).Select(p => new ClientBillPaymentDto(p.Amount, p.PaymentMethod, p.Status, p.CreatedAt)).ToList());
}

public class CommodityService : ICommodityService
{
    private readonly AgriCheckDbContext _db;

    public CommodityService(AgriCheckDbContext db) => _db = db;

    public async Task<IReadOnlyList<CommodityDto>> ListAsync(CancellationToken cancellationToken = default) =>
        await _db.Commodities.Include(c => c.Category).Where(c => c.IsActive)
            .OrderBy(c => c.Name)
            .Select(c => new CommodityDto(c.Id, c.Code, c.Name, c.Category.Name))
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<AgencyOptionDto>> ListAgenciesAsync(CancellationToken cancellationToken = default) =>
        await _db.Agencies.Where(a => a.IsActive && a.Code != "DA").OrderBy(a => a.Name)
            .Select(a => new AgencyOptionDto(a.Id, a.Code, a.Name))
            .ToListAsync(cancellationToken);
}

public class ClientProfileService : IClientProfileService
{
    private readonly AgriCheckDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public ClientProfileService(AgriCheckDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<ClientProfileDto> GetAsync(CancellationToken cancellationToken = default)
    {
        var user = await UserContextHelper.RequireUserAsync(_db, _currentUser, cancellationToken);
        await _db.Entry(user).Reference(u => u.Profile).LoadAsync(cancellationToken);
        return Map(user);
    }

    public async Task<ClientProfileDto> UpdateAsync(UpdateClientProfileRequest request, CancellationToken cancellationToken = default)
    {
        var user = await UserContextHelper.RequireUserAsync(_db, _currentUser, cancellationToken);
        await _db.Entry(user).Reference(u => u.Profile).LoadAsync(cancellationToken);

        if (user.Profile is null)
        {
            user.Profile = new UserProfile { UserId = user.Id };
            _db.UserProfiles.Add(user.Profile);
        }

        user.Profile.FirstName = request.FirstName.Trim();
        user.Profile.LastName = request.LastName.Trim();
        user.Profile.Phone = string.IsNullOrWhiteSpace(request.Phone) ? null : request.Phone.Trim();
        user.Profile.CompanyName = string.IsNullOrWhiteSpace(request.CompanyName) ? null : request.CompanyName.Trim();
        user.Profile.Address = string.IsNullOrWhiteSpace(request.Address) ? null : request.Address.Trim();
        user.Profile.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);
        return Map(user);
    }

    private static ClientProfileDto Map(User user) => new(
        user.Profile?.FirstName ?? string.Empty,
        user.Profile?.LastName ?? string.Empty,
        user.Email,
        user.Profile?.Phone,
        user.Profile?.CompanyName,
        user.Profile?.Address);
}

public class ClientContainerService : IClientContainerService
{
    private readonly AgriCheckDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public ClientContainerService(AgriCheckDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<ClientContainerListItemDto>> ListAsync(string? status, CancellationToken cancellationToken = default)
    {
        var user = await UserContextHelper.RequireUserAsync(_db, _currentUser, cancellationToken);
        var query = _db.Containers
            .Include(c => c.Entry).ThenInclude(e => e.Agency)
            .Where(c => c.Entry.UserId == user.Id);

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<ContainerStatus>(status, true, out var parsed))
        {
            query = query.Where(c => c.Status == parsed);
        }

        return await query
            .OrderByDescending(c => c.UpdatedAt)
            .Select(c => new ClientContainerListItemDto(
                c.Uuid,
                c.ContainerNumber,
                c.ContainerType,
                c.Status.ToString(),
                c.Entry.ReferenceNo,
                c.Entry.Agency.Code,
                c.UpdatedAt))
            .ToListAsync(cancellationToken);
    }

    public async Task<ClientContainerDetailDto?> GetAsync(Guid uuid, CancellationToken cancellationToken = default)
    {
        var user = await UserContextHelper.RequireUserAsync(_db, _currentUser, cancellationToken);
        var container = await _db.Containers
            .Include(c => c.Entry).ThenInclude(e => e.Agency)
            .Include(c => c.Inventories).ThenInclude(i => i.WarehouseFacility)
            .Include(c => c.TransportTags)
            .Where(c => c.Uuid == uuid && c.Entry.UserId == user.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (container is null)
        {
            return null;
        }

        var (canBookWarehouse, blockedReason) = ClientContainerProcessHelper.EvaluateBookingEligibility(container, container.Entry);

        var bookings = await _db.WarehouseBookings
            .Include(b => b.WarehouseFacility)
            .Where(b => b.UserId == user.Id &&
                        (b.ContainerReference == container.ContainerNumber ||
                         b.ContainerReference == container.Uuid.ToString()))
            .OrderByDescending(b => b.CreatedAt)
            .Select(b => new ClientContainerBookingSummaryDto(
                b.Uuid,
                b.BookingNumber,
                b.WarehouseFacility.Name,
                b.Status.ToString(),
                b.ScheduledDate))
            .ToListAsync(cancellationToken);

        var transportTag = container.TransportTags
            .OrderByDescending(t => t.TaggedAt)
            .FirstOrDefault();

        ClientContainerTransportTagDto? transportTagDto = transportTag is null
            ? null
            : new ClientContainerTransportTagDto(
                transportTag.Uuid,
                transportTag.ScheduledWarehouseDate.HasValue
                    ? DateOnly.FromDateTime(transportTag.ScheduledWarehouseDate.Value)
                    : null,
                transportTag.TaggedAt,
                transportTag.QrCodeData);

        return new ClientContainerDetailDto(
            container.Uuid,
            container.SequenceNumber,
            container.ContainerNumber,
            container.ContainerType,
            container.FormDataJson,
            container.Status.ToString(),
            container.Entry.Uuid,
            container.Entry.ReferenceNo,
            container.Entry.Status.ToString(),
            container.Entry.Agency.Code,
            container.Entry.Agency.Name,
            container.DepartureTime,
            container.ArrivalTime,
            container.CreatedAt,
            container.UpdatedAt,
            canBookWarehouse,
            blockedReason,
            ClientContainerProcessHelper.BuildProcessSteps(container),
            ClientContainerProcessHelper.MapWarehouseInfo(container),
            bookings,
            transportTagDto);
    }
}

public class ClientInspectionService : IClientInspectionService
{
    private readonly AgriCheckDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public ClientInspectionService(AgriCheckDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<ClientInspectionListItemDto>> ListAsync(string? status, Guid? entryUuid, CancellationToken cancellationToken = default)
    {
        var user = await UserContextHelper.RequireUserAsync(_db, _currentUser, cancellationToken);
        var query = _db.Inspections
            .Include(i => i.Entry).ThenInclude(e => e.Agency)
            .Where(i => i.Entry.UserId == user.Id);

        if (entryUuid is not null)
            query = query.Where(i => i.Entry.Uuid == entryUuid);

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<InspectionStatus>(status, true, out var parsed))
            query = query.Where(i => i.Status == parsed);

        return await query
            .OrderByDescending(i => i.UpdatedAt)
            .Select(i => new ClientInspectionListItemDto(
                i.Uuid,
                i.Entry.Uuid,
                i.Entry.ReferenceNo,
                i.Entry.Agency.Code,
                i.Status.ToString(),
                i.ScheduledAt,
                i.CompletedAt))
            .ToListAsync(cancellationToken);
    }

    public async Task<ClientInspectionDetailDto?> GetAsync(Guid uuid, CancellationToken cancellationToken = default)
    {
        var user = await UserContextHelper.RequireUserAsync(_db, _currentUser, cancellationToken);
        return await _db.Inspections
            .Include(i => i.Entry).ThenInclude(e => e.Agency)
            .Include(i => i.Inspector).ThenInclude(u => u.Profile)
            .Where(i => i.Uuid == uuid && i.Entry.UserId == user.Id)
            .Select(i => new ClientInspectionDetailDto(
                i.Uuid,
                i.Entry.Uuid,
                i.Entry.ReferenceNo,
                i.Entry.Agency.Code,
                i.Entry.Agency.Name,
                i.Status.ToString(),
                i.ScheduledAt,
                i.CompletedAt,
                i.Findings,
                i.Inspector.Profile != null ? $"{i.Inspector.Profile.FirstName} {i.Inspector.Profile.LastName}" : i.Inspector.Email))
            .FirstOrDefaultAsync(cancellationToken);
    }
}

public class ClientDashboardService : IClientDashboardService
{
    private readonly AgriCheckDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public ClientDashboardService(AgriCheckDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<ClientDashboardDto> GetDashboardAsync(CancellationToken cancellationToken = default)
    {
        var user = await UserContextHelper.RequireUserAsync(_db, _currentUser, cancellationToken);
        await _db.Entry(user).Reference(u => u.Profile).LoadAsync(cancellationToken);

        var entries = _db.Entries.Where(e => e.UserId == user.Id);
        var entryIds = entries.Select(e => e.Id);
        var bills = _db.ClientBills.Where(b => b.UserId == user.Id);
        var openBills = bills.Where(b => b.Status != ClientBillStatus.Paid && b.Status != ClientBillStatus.Cancelled);
        var containers = _db.Containers.Where(c => entryIds.Contains(c.EntryId));
        var inspections = _db.Inspections.Where(i => entryIds.Contains(i.EntryId));

        var latestAccreditation = await _db.AccreditationSubmissions
            .AsNoTracking()
            .Include(s => s.History)
            .Where(s => s.UserId == user.Id)
            .OrderByDescending(s => s.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        var isAccredited = latestAccreditation?.Status == AccreditationSubmissionStatus.Approved;

        var agencies = await _db.Agencies
            .Where(a => a.IsActive && a.Code != "DA")
            .OrderBy(a => a.Name)
            .Select(a => new ClientDashboardAgencyDto(
                a.Id,
                a.Code,
                a.Name,
                a.LogoUrl,
                _db.FormTemplates.Any(t =>
                    t.IsActive
                    && t.Status == FormTemplateStatus.Published
                    && t.FormType == "ENTRY"
                    && t.AgencyTags.Any(tag => tag.AgencyId == a.Id))))
            .ToListAsync(cancellationToken);

        var now = DateTime.UtcNow;
        var recentBills = await openBills
            .OrderByDescending(b => b.CreatedAt)
            .Take(5)
            .Select(b => new ClientDashboardRecentBillDto(
                b.Uuid,
                b.Entry != null ? b.Entry.Uuid : null,
                b.BillNumber,
                b.Entry != null ? b.Entry.ReferenceNo : null,
                b.Entry != null ? b.Entry.Agency.Name : null,
                b.Description,
                b.Amount,
                b.Status.ToString(),
                b.DueDate,
                b.Status == ClientBillStatus.Overdue || (b.DueDate != null && b.DueDate < now && b.Status != ClientBillStatus.Paid),
                b.PaymentLinkToken))
            .ToListAsync(cancellationToken);

        var recentEntries = await entries
            .OrderByDescending(e => e.CreatedAt)
            .Take(5)
            .Select(e => new ClientDashboardRecentEntryDto(
                e.Uuid,
                e.ReferenceNo,
                e.Status.ToString(),
                e.Agency.Code,
                e.CreatedAt))
            .ToListAsync(cancellationToken);

        var recentCertificates = await _db.Certificates
            .Where(c => c.UserId == user.Id)
            .OrderByDescending(c => c.IssuedAt)
            .Take(3)
            .Select(c => new ClientDashboardRecentCertificateDto(
                c.Uuid,
                c.CertificateNumber,
                c.Title,
                c.Status.ToString(),
                c.IssuedAt))
            .ToListAsync(cancellationToken);

        var entryStatusCounts = await entries
            .GroupBy(e => e.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);
        var entryCountByStatus = entryStatusCounts.ToDictionary(x => x.Status, x => x.Count);
        int EntryCount(EntryStatus status) => entryCountByStatus.GetValueOrDefault(status);

        var pendingPayments = await entries.CountAsync(e => e.PaymentStatus == PaymentStatus.Pending, cancellationToken);
        var pendingAgencyBillings = await _db.AgencyBillings.CountAsync(
            b => b.Entry != null && b.Entry.UserId == user.Id && b.Status == AgencyBillingStatus.PaymentPending,
            cancellationToken);
        var unpaidBillCount = await openBills.CountAsync(cancellationToken);
        var overdueBillCount = await openBills.CountAsync(b => b.Status == ClientBillStatus.Overdue, cancellationToken);
        var unpaidBillTotal = await openBills.SumAsync(b => (decimal?)b.Amount, cancellationToken) ?? 0m;
        var approvedContainers = await containers.CountAsync(
            c => c.Status == ContainerStatus.AtWarehouse || c.Status == ContainerStatus.Released,
            cancellationToken);
        var assignedContainers = await containers.CountAsync(c => c.AssignedDriverUserId != null, cancellationToken);
        var pendingInspections = await inspections.CountAsync(
            i => i.Status == InspectionStatus.Scheduled || i.Status == InspectionStatus.InProgress,
            cancellationToken);

        return new ClientDashboardDto(
            new ClientDashboardProfileDto(
                user.Profile?.FirstName ?? string.Empty,
                user.Profile?.LastName ?? string.Empty,
                user.Email),
            new ClientDashboardAccreditationDto(
                latestAccreditation?.Status.ToString(),
                latestAccreditation is null ? null : AccreditationSubmissionStatusMapper.GetDisplayStatus(latestAccreditation),
                latestAccreditation?.SubmissionType,
                latestAccreditation?.AccreditationNumber,
                latestAccreditation?.CompanyName,
                latestAccreditation?.ReviewComments,
                isAccredited),
            new ClientDashboardEntryStatsDto(
                entryCountByStatus.Values.Sum(),
                EntryCount(EntryStatus.Submitted) + EntryCount(EntryStatus.UnderReview),
                EntryStatusRules.OperationalPipelineStatuses.Sum(EntryCount),
                EntryCount(EntryStatus.ForCompliance)),
            new ClientDashboardWorkflowStatsDto(
                EntryCount(EntryStatus.DaIssueBilling),
                EntryCount(EntryStatus.ForInspection),
                EntryCount(EntryStatus.ReadyForTransport),
                EntryCount(EntryStatus.AwaitingTransport) + EntryCount(EntryStatus.PartiallyConfirmed),
                EntryCount(EntryStatus.InTransit),
                pendingAgencyBillings),
            new ClientDashboardLogisticsStatsDto(
                pendingPayments,
                unpaidBillCount,
                overdueBillCount,
                unpaidBillTotal,
                approvedContainers,
                assignedContainers,
                pendingInspections),
            agencies,
            recentBills,
            recentEntries,
            recentCertificates);
    }
}
