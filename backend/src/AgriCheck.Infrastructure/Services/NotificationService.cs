using AgriCheck.Application.ClientPortal;
using AgriCheck.Application.Notifications;
using AgriCheck.Domain.Entities;
using AgriCheck.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AgriCheck.Infrastructure.Services;

public class NotificationService : INotificationService
{
    private readonly AgriCheckDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly Application.Common.IEmailService _emailService;

    public NotificationService(
        AgriCheckDbContext db,
        ICurrentUserService currentUser,
        Application.Common.IEmailService emailService)
    {
        _db = db;
        _currentUser = currentUser;
        _emailService = emailService;
    }

    public async Task<IReadOnlyList<NotificationDto>> ListAsync(int limit, bool unreadOnly, CancellationToken cancellationToken = default)
    {
        var user = await UserContextHelper.RequireUserAsync(_db, _currentUser, cancellationToken);
        var take = Math.Clamp(limit, 1, 50);
        var query = _db.Notifications.Where(n => n.UserId == user.Id);
        if (unreadOnly) query = query.Where(n => !n.IsRead);

        return await query
            .OrderByDescending(n => n.CreatedAt)
            .Take(take)
            .Select(n => Map(n))
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetUnreadCountAsync(CancellationToken cancellationToken = default)
    {
        var user = await UserContextHelper.RequireUserAsync(_db, _currentUser, cancellationToken);
        return await _db.Notifications.CountAsync(n => n.UserId == user.Id && !n.IsRead, cancellationToken);
    }

    public async Task MarkReadAsync(Guid uuid, CancellationToken cancellationToken = default)
    {
        var user = await UserContextHelper.RequireUserAsync(_db, _currentUser, cancellationToken);
        var notification = await _db.Notifications.FirstOrDefaultAsync(n => n.Uuid == uuid && n.UserId == user.Id, cancellationToken)
            ?? throw new ClientPortalException("NOT_FOUND", "Notification not found.");
        notification.IsRead = true;
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task MarkAllReadAsync(CancellationToken cancellationToken = default)
    {
        var user = await UserContextHelper.RequireUserAsync(_db, _currentUser, cancellationToken);
        await _db.Notifications
            .Where(n => n.UserId == user.Id && !n.IsRead)
            .ExecuteUpdateAsync(s => s.SetProperty(n => n.IsRead, true), cancellationToken);
    }

    public async Task<NotificationPreferenceDto> GetPreferencesAsync(CancellationToken cancellationToken = default)
    {
        var user = await UserContextHelper.RequireUserAsync(_db, _currentUser, cancellationToken);
        var prefs = await GetOrCreatePreferencesAsync(user.Id, cancellationToken);
        return new NotificationPreferenceDto(prefs.EmailEnabled, prefs.InAppEnabled);
    }

    public async Task<NotificationPreferenceDto> UpdatePreferencesAsync(UpdateNotificationPreferenceRequest request, CancellationToken cancellationToken = default)
    {
        var user = await UserContextHelper.RequireUserAsync(_db, _currentUser, cancellationToken);
        var prefs = await GetOrCreatePreferencesAsync(user.Id, cancellationToken);
        prefs.EmailEnabled = request.EmailEnabled;
        prefs.InAppEnabled = request.InAppEnabled;
        await _db.SaveChangesAsync(cancellationToken);
        return new NotificationPreferenceDto(prefs.EmailEnabled, prefs.InAppEnabled);
    }

    public async Task NotifyAsync(long userId, string type, string title, string message, string? relatedEntityType = null, string? relatedEntityUuid = null, CancellationToken cancellationToken = default)
    {
        var prefs = await GetOrCreatePreferencesAsync(userId, cancellationToken);

        if (prefs.InAppEnabled)
        {
            _db.Notifications.Add(new Notification
            {
                Uuid = Guid.NewGuid(),
                UserId = userId,
                Type = type,
                Title = title,
                Message = message,
                RelatedEntityType = relatedEntityType,
                RelatedEntityUuid = relatedEntityUuid
            });
            await _db.SaveChangesAsync(cancellationToken);
        }

        if (prefs.EmailEnabled && await _emailService.IsEnabledAsync(cancellationToken))
        {
            var email = await _db.Users.Where(u => u.Id == userId).Select(u => u.Email).FirstOrDefaultAsync(cancellationToken);
            if (!string.IsNullOrWhiteSpace(email))
            {
                var html = $"""
                    <p>{System.Net.WebUtility.HtmlEncode(message)}</p>
                    <p style="color:#666;font-size:12px">AgriCheck notification · {System.Net.WebUtility.HtmlEncode(type)}</p>
                    """;
                try
                {
                    await _emailService.SendAsync(email, title, html, cancellationToken);
                }
                catch
                {
                    // Email failures must not break core workflows.
                }
            }
        }
    }

    private async Task<NotificationPreference> GetOrCreatePreferencesAsync(long userId, CancellationToken cancellationToken)
    {
        var prefs = await _db.NotificationPreferences.FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);
        if (prefs is not null) return prefs;

        prefs = new NotificationPreference { UserId = userId };
        _db.NotificationPreferences.Add(prefs);
        await _db.SaveChangesAsync(cancellationToken);
        return prefs;
    }

    private static NotificationDto Map(Notification n) => new(
        n.Uuid, n.Type, n.Title, n.Message, n.RelatedEntityType, n.RelatedEntityUuid, n.IsRead, n.CreatedAt);
}
