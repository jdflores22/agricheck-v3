using Microsoft.Extensions.Configuration;

namespace AgriCheck.Infrastructure.Helpers;

public static class TransportQrSigningKeyResolver
{
    public static string Resolve(IConfiguration configuration)
    {
        var signingKey = configuration["App:QrSigningKey"]
            ?? configuration["Jwt:SecretKey"];
        if (string.IsNullOrWhiteSpace(signingKey))
        {
            throw new InvalidOperationException("QR signing key is not configured.");
        }

        if (signingKey.Length < 32)
        {
            throw new InvalidOperationException("QR signing key must be at least 32 characters.");
        }

        return signingKey;
    }
}
