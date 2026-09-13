using AgriCheck.Application.ClientPortal;
using AgriCheck.Application.Notifications;
using AgriCheck.Application.OpsPortal;
using AgriCheck.Application.OpsPortal.Dtos;
using AgriCheck.Domain.Entities;
using AgriCheck.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace AgriCheck.Infrastructure.Services;

public class AgriTrackPushService : IAgriTrackPushService
{
    private readonly AgriCheckDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly INotificationService _notifications;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<AgriTrackPushService> _logger;

    public AgriTrackPushService(
        AgriCheckDbContext db,
        ICurrentUserService currentUser,
        INotificationService notifications,
        IHttpClientFactory httpClientFactory,
        ILogger<AgriTrackPushService> logger)
    {
        _db = db;
        _currentUser = currentUser;
        _notifications = notifications;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task RegisterDeviceAsync(long userId, string expoPushToken, string platform, CancellationToken cancellationToken = default)
    {
        var token = expoPushToken.Trim();
        if (string.IsNullOrWhiteSpace(token))
        {
            throw new ClientPortalException("INVALID_TOKEN", "Expo push token is required.");
        }

        var existing = await _db.MobilePushDevices.FirstOrDefaultAsync(d => d.ExpoPushToken == token, cancellationToken);
        if (existing is null)
        {
            _db.MobilePushDevices.Add(new MobilePushDevice
            {
                UserId = userId,
                ExpoPushToken = token,
                Platform = platform.Trim(),
                LastSeenAt = DateTime.UtcNow
            });
        }
        else
        {
            existing.UserId = userId;
            existing.Platform = platform.Trim();
            existing.LastSeenAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task RemoveDeviceAsync(long userId, string expoPushToken, CancellationToken cancellationToken = default)
    {
        var token = expoPushToken.Trim();
        var device = await _db.MobilePushDevices
            .FirstOrDefaultAsync(d => d.ExpoPushToken == token && d.UserId == userId, cancellationToken);
        if (device is null)
        {
            return;
        }

        _db.MobilePushDevices.Remove(device);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task NotifyDriverAssignmentAsync(
        long driverUserId,
        string containerNumber,
        Guid containerUuid,
        CancellationToken cancellationToken = default)
    {
        var title = "New container assignment";
        var message = $"You were assigned container {containerNumber}. Open AgriTrack to view details.";

        await _notifications.NotifyAsync(
            driverUserId,
            "agritrack_assignment",
            title,
            message,
            "container",
            containerUuid.ToString(),
            cancellationToken);

        var tokens = await _db.MobilePushDevices
            .Where(d => d.UserId == driverUserId)
            .Select(d => d.ExpoPushToken)
            .ToListAsync(cancellationToken);

        if (tokens.Count == 0)
        {
            return;
        }

        var payload = tokens.Select(t => new ExpoPushMessage
        {
            To = t,
            Title = title,
            Body = message,
            Data = new Dictionary<string, string>
            {
                ["type"] = "agritrack_assignment",
                ["containerUuid"] = containerUuid.ToString(),
                ["containerNumber"] = containerNumber
            }
        }).ToList();

        try
        {
            var client = _httpClientFactory.CreateClient("ExpoPush");
            var response = await client.PostAsJsonAsync("https://exp.host/--/api/v2/push/send", payload, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Expo push send failed with status {StatusCode}", response.StatusCode);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send Expo push notification for container {ContainerUuid}", containerUuid);
        }
    }

    public async Task RegisterCurrentUserDeviceAsync(RegisterMobilePushDeviceRequest request, CancellationToken cancellationToken = default)
    {
        var user = await OpsContextHelper.RequireMobileUserAsync(_db, _currentUser, cancellationToken);
        await RegisterDeviceAsync(user.Id, request.ExpoPushToken, request.Platform, cancellationToken);
    }

    public async Task RemoveCurrentUserDeviceAsync(string expoPushToken, CancellationToken cancellationToken = default)
    {
        var user = await OpsContextHelper.RequireMobileUserAsync(_db, _currentUser, cancellationToken);
        await RemoveDeviceAsync(user.Id, expoPushToken, cancellationToken);
    }

    public async Task NotifyWarehouseArrivalAsync(
        string containerNumber,
        Guid containerUuid,
        string warehouseName,
        CancellationToken cancellationToken = default)
    {
        var doctorUserIds = await _db.UserRoles
            .Where(ur => ur.Role.Code == "ROLE_DOCTOR")
            .Select(ur => ur.UserId)
            .Distinct()
            .ToListAsync(cancellationToken);

        var title = "Driver arrived at warehouse";
        var message = $"Container {containerNumber} checked in at {warehouseName}.";
        foreach (var userId in doctorUserIds)
        {
            await _notifications.NotifyAsync(
                userId,
                "agritrack_arrival",
                title,
                message,
                "container",
                containerUuid.ToString(),
                cancellationToken);
        }

        var tokens = await _db.MobilePushDevices
            .Where(d => doctorUserIds.Contains(d.UserId))
            .Select(d => d.ExpoPushToken)
            .ToListAsync(cancellationToken);
        if (tokens.Count == 0)
        {
            return;
        }

        var payload = tokens.Select(t => new ExpoPushMessage
        {
            To = t,
            Title = title,
            Body = message,
            Data = new Dictionary<string, string>
            {
                ["type"] = "agritrack_arrival",
                ["containerUuid"] = containerUuid.ToString(),
                ["containerNumber"] = containerNumber,
            }
        }).ToList();

        try
        {
            var client = _httpClientFactory.CreateClient("ExpoPush");
            await client.PostAsJsonAsync("https://exp.host/--/api/v2/push/send", payload, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send warehouse arrival push for container {ContainerUuid}", containerUuid);
        }
    }

    private sealed class ExpoPushMessage
    {
        [JsonPropertyName("to")]
        public string To { get; set; } = string.Empty;

        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;

        [JsonPropertyName("body")]
        public string Body { get; set; } = string.Empty;

        [JsonPropertyName("data")]
        public Dictionary<string, string> Data { get; set; } = new();
    }
}
