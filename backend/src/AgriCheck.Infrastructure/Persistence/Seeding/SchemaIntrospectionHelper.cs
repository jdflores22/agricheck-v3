using System.Data.Common;

namespace AgriCheck.Infrastructure.Persistence.Seeding;

/// <summary>
/// Schema checks that work on Hostinger (no information_schema access).
/// </summary>
internal static class SchemaIntrospectionHelper
{
    public static async Task<bool> TableExistsAsync(
        DbConnection connection,
        string tableName,
        CancellationToken cancellationToken = default)
    {
        await EnsureOpenAsync(connection, cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = $"SHOW TABLES LIKE '{EscapeLiteral(tableName)}'";

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken);
    }

    public static async Task<bool> ColumnExistsAsync(
        DbConnection connection,
        string tableName,
        string columnName,
        CancellationToken cancellationToken = default)
    {
        await EnsureOpenAsync(connection, cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText =
            $"SHOW COLUMNS FROM `{EscapeIdentifier(tableName)}` LIKE '{EscapeLiteral(columnName)}'";

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken);
    }

    public static async Task<bool> IndexExistsAsync(
        DbConnection connection,
        string tableName,
        string indexName,
        CancellationToken cancellationToken = default)
    {
        await EnsureOpenAsync(connection, cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = $"SHOW INDEX FROM `{EscapeIdentifier(tableName)}`";

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var keyName = reader.GetString(reader.GetOrdinal("Key_name"));
            if (string.Equals(keyName, indexName, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    public static async Task<bool> ForeignKeyExistsAsync(
        DbConnection connection,
        string tableName,
        string constraintName,
        CancellationToken cancellationToken = default)
    {
        await EnsureOpenAsync(connection, cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = $"SHOW CREATE TABLE `{EscapeIdentifier(tableName)}`";

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return false;
        }

        var createSql = reader.GetString(1);
        return createSql.Contains(
            $"CONSTRAINT `{constraintName}`",
            StringComparison.OrdinalIgnoreCase);
    }

    public static async Task<int> CountApplicationTablesAsync(
        DbConnection connection,
        CancellationToken cancellationToken = default)
    {
        await EnsureOpenAsync(connection, cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = "SHOW TABLES";

        var count = 0;
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var tableName = reader.GetString(0);
            if (!string.Equals(tableName, "__EFMigrationsHistory", StringComparison.OrdinalIgnoreCase))
            {
                count++;
            }
        }

        return count;
    }

    private static async Task EnsureOpenAsync(DbConnection connection, CancellationToken cancellationToken)
    {
        if (connection.State != System.Data.ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken);
        }
    }

    private static string EscapeIdentifier(string value) => value.Replace("`", "``", StringComparison.Ordinal);

    private static string EscapeLiteral(string value) => value.Replace("'", "''", StringComparison.Ordinal);
}
