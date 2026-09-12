using Microsoft.Extensions.Configuration;

namespace AgriCheck.Infrastructure.Persistence;

public static class DatabaseConnection
{
    public static string Resolve(IConfiguration configuration)
    {
        var host = First(configuration, "MYSQL_HOST", "MYSQLHOST");
        var database = First(configuration, "MYSQL_DATABASE", "MYSQLDATABASE");
        var user = First(configuration, "MYSQL_USER", "MYSQLUSER");
        var password = First(configuration, "MYSQL_PASSWORD", "MYSQLPASSWORD") ?? "";
        var port = First(configuration, "MYSQL_PORT", "MYSQLPORT") ?? "3306";
        var sslMode = First(configuration, "MYSQL_SSL_MODE") ?? "None";

        if (!string.IsNullOrWhiteSpace(host)
            && !string.IsNullOrWhiteSpace(database)
            && !string.IsNullOrWhiteSpace(user))
        {
            return $"Server={host};Port={port};Database={database};User={user};Password={password};SslMode={sslMode};AllowPublicKeyRetrieval=True;Connection Timeout=10;";
        }

        return configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "Set MYSQL_HOST, MYSQL_DATABASE, MYSQL_USER, and MYSQL_PASSWORD, or ConnectionStrings:DefaultConnection.");
    }

    private static string? First(IConfiguration configuration, params string[] keys)
    {
        foreach (var key in keys)
        {
            var value = configuration[key];
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value.Trim();
            }
        }

        return null;
    }
}
