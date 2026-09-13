using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace AgriCheck.Infrastructure.Helpers;

public sealed record TransportQrPayload(
    Guid TagUuid,
    Guid ContainerUuid,
    string ContainerNumber,
    string EntryReference,
    string TransportType,
    DateOnly? ScheduledWarehouseDate,
    DateTime Timestamp);

public static class TransportQrCodec
{
    public const string PayloadType = "agricheck_transport";
    public const string PayloadVersion = "1";
    public static readonly TimeSpan MaxAge = TimeSpan.FromHours(48);
    public static readonly TimeSpan FutureSkew = TimeSpan.FromMinutes(5);

    public static string Encode(TransportQrPayload payload, string signingKey)
    {
        var data = new SortedDictionary<string, string>(StringComparer.Ordinal)
        {
            ["type"] = PayloadType,
            ["version"] = PayloadVersion,
            ["tag_uuid"] = payload.TagUuid.ToString(),
            ["container_uuid"] = payload.ContainerUuid.ToString(),
            ["container_number"] = payload.ContainerNumber,
            ["entry_reference"] = payload.EntryReference,
            ["transport_type"] = payload.TransportType,
            ["scheduled_warehouse_date"] = payload.ScheduledWarehouseDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? string.Empty,
            ["timestamp"] = payload.Timestamp.ToUniversalTime().ToString("O"),
        };

        data["signature"] = Sign(data, signingKey);
        return JsonSerializer.Serialize(data);
    }

    public static TransportQrPayload Verify(string qrData, string signingKey)
    {
        if (string.IsNullOrWhiteSpace(qrData))
        {
            throw new InvalidOperationException("QR data is required.");
        }

        using var document = JsonDocument.Parse(qrData);
        var root = document.RootElement;
        if (root.ValueKind != JsonValueKind.Object)
        {
            throw new InvalidOperationException("Invalid transport QR format.");
        }

        var type = GetRequiredString(root, "type");
        if (!string.Equals(type, PayloadType, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("This QR code is not an AgriCheck transport tag.");
        }

        var version = GetRequiredString(root, "version");
        if (!string.Equals(version, PayloadVersion, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Unsupported transport QR version.");
        }

        var signature = GetRequiredString(root, "signature");
        var data = new SortedDictionary<string, string>(StringComparer.Ordinal);
        foreach (var property in root.EnumerateObject())
        {
            if (property.NameEquals("signature"))
            {
                continue;
            }

            data[property.Name] = property.Value.ValueKind switch
            {
                JsonValueKind.String => property.Value.GetString() ?? string.Empty,
                JsonValueKind.Number => property.Value.GetRawText(),
                JsonValueKind.True => "true",
                JsonValueKind.False => "false",
                _ => property.Value.GetRawText(),
            };
        }

        var expected = Sign(data, signingKey);
        if (!TryFixedTimeEqualsHex(signature, expected))
        {
            throw new InvalidOperationException("Invalid transport QR signature.");
        }

        if (!Guid.TryParse(GetRequiredString(root, "tag_uuid"), out var tagUuid)
            || !Guid.TryParse(GetRequiredString(root, "container_uuid"), out var containerUuid))
        {
            throw new InvalidOperationException("Invalid transport QR identifiers.");
        }

        if (!DateTime.TryParse(
                GetRequiredString(root, "timestamp"),
                CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind,
                out var timestamp))
        {
            throw new InvalidOperationException("Invalid transport QR timestamp.");
        }

        var age = DateTime.UtcNow - timestamp.ToUniversalTime();
        if (age > MaxAge || age < -FutureSkew)
        {
            throw new InvalidOperationException("Transport QR has expired.");
        }

        DateOnly? scheduledWarehouseDate = null;
        if (root.TryGetProperty("scheduled_warehouse_date", out var scheduledDateElement)
            && scheduledDateElement.ValueKind == JsonValueKind.String
            && DateOnly.TryParse(scheduledDateElement.GetString(), CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedDate))
        {
            scheduledWarehouseDate = parsedDate;
        }

        return new TransportQrPayload(
            tagUuid,
            containerUuid,
            GetRequiredString(root, "container_number"),
            GetRequiredString(root, "entry_reference"),
            GetRequiredString(root, "transport_type"),
            scheduledWarehouseDate,
            timestamp);
    }

    private static bool TryFixedTimeEqualsHex(string left, string right)
    {
        try
        {
            var leftBytes = Convert.FromHexString(left);
            var rightBytes = Convert.FromHexString(right);
            return CryptographicOperations.FixedTimeEquals(leftBytes, rightBytes);
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static string Sign(SortedDictionary<string, string> data, string signingKey)
    {
        var canonical = JsonSerializer.Serialize(data);
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(signingKey));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(canonical));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static string GetRequiredString(JsonElement root, string name)
    {
        if (!root.TryGetProperty(name, out var value) || value.ValueKind != JsonValueKind.String)
        {
            throw new InvalidOperationException($"Transport QR is missing field: {name}.");
        }

        var text = value.GetString();
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new InvalidOperationException($"Transport QR field {name} is empty.");
        }

        return text;
    }
}
