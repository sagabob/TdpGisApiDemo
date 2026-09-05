using System.Data.Common;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using Npgsql;
using TdpGis.AdminApplication.Abstractions;
using TdpGis.Domain;

namespace TdpGis.Infrastructure.Sql;

public sealed class SqlMetadataProvider(PostgresDataSourceCache postgresDataSources) : ISqlMetadataProvider
{
    private static readonly JsonSerializerOptions SampleJsonOptions = new() { WriteIndented = true };

    public async Task<SqlConnectionProbeResult> ProbeConnectionAsync(SourceType databaseType, string connectionString,
        string? tableName, CancellationToken cancellationToken = default)
    {
        if (!databaseType.IsRelational())
            return new SqlConnectionProbeResult(false, $"Unsupported SQL database type '{databaseType}'.", null, null);

        if (string.IsNullOrWhiteSpace(connectionString))
            return new SqlConnectionProbeResult(false,
                $"{databaseType.ToDisplayName()} connection string is required.", null, null);

        try
        {
            await using var connection = CreateConnection(databaseType, connectionString);
            await connection.OpenAsync(cancellationToken);
            var tables = await ListTableNamesAsync(connection, databaseType, cancellationToken);

            if (!string.IsNullOrWhiteSpace(tableName) &&
                !tables.Contains(tableName.Trim(), StringComparer.OrdinalIgnoreCase))
                return new SqlConnectionProbeResult(false, $"Table '{tableName}' was not found.", null, null);

            return new SqlConnectionProbeResult(true, string.Empty, connection.Database, tables);
        }
        catch (Exception ex)
        {
            return new SqlConnectionProbeResult(false, FormatFailure(databaseType, ex), null, null);
        }
    }

    public async Task<IReadOnlyList<string>> ListTableNamesAsync(SourceType databaseType, string connectionString,
        CancellationToken cancellationToken = default)
    {
        await using var connection = CreateConnection(databaseType, connectionString);
        await connection.OpenAsync(cancellationToken);
        return await ListTableNamesAsync(connection, databaseType, cancellationToken);
    }

    public async Task<SqlRowSampleResult> GetSampleRowAsync(SourceType databaseType, string connectionString,
        string tableName, CancellationToken cancellationToken = default)
    {
        if (!databaseType.IsRelational())
            throw new ArgumentOutOfRangeException(nameof(databaseType), databaseType, null);

        var (schema, name) = SqlIdentifiers.SplitTableName(tableName, databaseType);
        await using var connection = CreateConnection(databaseType, connectionString);
        await connection.OpenAsync(cancellationToken);

        var sql = databaseType == SourceType.SqlServer
            ? await BuildSqlServerSampleQueryAsync(connection, schema, name, cancellationToken)
            : await BuildPostgresSampleQueryAsync(connection, schema, name, cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
            return new SqlRowSampleResult(false, [], "{}");

        var fields = new string[reader.FieldCount];
        var values = new Dictionary<string, object?>(StringComparer.Ordinal);
        for (var i = 0; i < reader.FieldCount; i++)
        {
            fields[i] = reader.GetName(i);
            values[fields[i]] = ReadSampleField(reader, i);
        }

        return new SqlRowSampleResult(true, fields, JsonSerializer.Serialize(values, SampleJsonOptions));
    }

    private static async Task<string> BuildSqlServerSampleQueryAsync(
        DbConnection connection, string schema, string name, CancellationToken cancellationToken)
    {
        var columns = await LoadColumnsAsync(
            connection,
            """
            SELECT c.name AS ColumnName, t.name AS TypeName
            FROM sys.columns c
            INNER JOIN sys.types t ON c.user_type_id = t.user_type_id
            WHERE c.object_id = OBJECT_ID(QUOTENAME(@schema) + N'.' + QUOTENAME(@name))
            ORDER BY c.column_id
            """,
            schema,
            name,
            cancellationToken);

        var from = $"{SqlIdentifiers.QuoteSqlServer(schema)}.{SqlIdentifiers.QuoteSqlServer(name)}";
        if (columns.Count == 0)
            return $"SELECT TOP 1 * FROM {from}";

        var selectList = string.Join(", ", columns.Select(c =>
        {
            var quoted = SqlIdentifiers.QuoteSqlServer(c.Name);
            return c.TypeName.ToLowerInvariant() switch
            {
                "geometry" or "geography" => $"CONVERT(nvarchar(max), {quoted}.STAsText()) AS {quoted}",
                "hierarchyid" => $"CONVERT(nvarchar(4000), {quoted}) AS {quoted}",
                "image" or "sql_variant" or "xml" => $"CONVERT(nvarchar(max), {quoted}) AS {quoted}",
                _ => quoted
            };
        }));

        return $"SELECT TOP 1 {selectList} FROM {from}";
    }

    private static async Task<string> BuildPostgresSampleQueryAsync(
        DbConnection connection, string schema, string name, CancellationToken cancellationToken)
    {
        var columns = await LoadColumnsAsync(
            connection,
            """
            SELECT a.attname AS column_name, t.typname AS type_name
            FROM pg_catalog.pg_attribute a
            INNER JOIN pg_catalog.pg_class c ON a.attrelid = c.oid
            INNER JOIN pg_catalog.pg_namespace n ON c.relnamespace = n.oid
            INNER JOIN pg_catalog.pg_type t ON a.atttypid = t.oid
            WHERE n.nspname = @schema
              AND c.relname = @name
              AND a.attnum > 0
              AND NOT a.attisdropped
            ORDER BY a.attnum
            """,
            schema,
            name,
            cancellationToken);

        var from = $"{SqlIdentifiers.QuotePostgres(schema)}.{SqlIdentifiers.QuotePostgres(name)}";
        if (columns.Count == 0)
            return $"SELECT * FROM {from} LIMIT 1";

        var selectList = string.Join(", ", columns.Select(c =>
        {
            var quoted = SqlIdentifiers.QuotePostgres(c.Name);
            return c.TypeName.ToLowerInvariant() switch
            {
                "geometry" or "geography" => $"ST_AsText({quoted}) AS {quoted}",
                _ => quoted
            };
        }));

        return $"SELECT {selectList} FROM {from} LIMIT 1";
    }

    private static async Task<IReadOnlyList<(string Name, string TypeName)>> LoadColumnsAsync(
        DbConnection connection,
        string sql,
        string schema,
        string name,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        AddParameter(command, "@schema", schema);
        AddParameter(command, "@name", name);

        var columns = new List<(string Name, string TypeName)>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
            columns.Add((reader.GetString(0), reader.GetString(1)));

        return columns;
    }

    /// <summary>
    ///     Never call GetValue() on geometry/geography — that loads Microsoft.SqlServer.Types.
    /// </summary>
    private static object? ReadSampleField(DbDataReader reader, int ordinal)
    {
        if (reader.IsDBNull(ordinal))
            return null;

        var typeName = reader.GetDataTypeName(ordinal);
        if (typeName.Contains("geometry", StringComparison.OrdinalIgnoreCase) ||
            typeName.Contains("geography", StringComparison.OrdinalIgnoreCase))
            return "(spatial)";

        try
        {
            return ConvertSampleValue(reader.GetValue(ordinal));
        }
        catch (Exception ex) when (ex is FileNotFoundException or FileLoadException or InvalidCastException)
        {
            return $"({typeName})";
        }
    }

    private static object? ConvertSampleValue(object value) => value switch
    {
        string or bool or byte or short or int or long or float or double or decimal => value,
        DateTime or DateTimeOffset or Guid or TimeSpan => value,
        byte[] bytes => Convert.ToHexString(bytes),
        _ => value.ToString()
    };

    private static async Task<IReadOnlyList<string>> ListTableNamesAsync(DbConnection connection,
        SourceType databaseType, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = databaseType == SourceType.SqlServer
            ? """
              SELECT s.name + N'.' + o.name AS qualified_name
              FROM sys.objects o
              INNER JOIN sys.schemas s ON o.schema_id = s.schema_id
              WHERE o.type IN (N'U', N'V')
                AND o.is_ms_shipped = 0
                AND s.name NOT IN (N'sys', N'INFORMATION_SCHEMA', N'guest')
              ORDER BY s.name, o.name
              """
            : """
              SELECT n.nspname || '.' || c.relname AS qualified_name
              FROM pg_catalog.pg_class c
              JOIN pg_catalog.pg_namespace n ON n.oid = c.relnamespace
              WHERE c.relkind IN ('r', 'p', 'v', 'm', 'f')
                AND n.nspname NOT IN ('pg_catalog', 'information_schema', 'pg_toast')
                AND n.nspname NOT LIKE 'pg_temp_%'
                AND n.nspname NOT LIKE 'pg_toast_temp_%'
              ORDER BY n.nspname, c.relname
              """;

        var tables = new List<string>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            if (!reader.IsDBNull(0))
                tables.Add(reader.GetString(0));
        }

        return tables;
    }

    private DbConnection CreateConnection(SourceType databaseType, string connectionString)
    {
        var trimmed = connectionString.Trim();
        return databaseType switch
        {
            SourceType.Postgres => postgresDataSources.CreateConnection(trimmed),
            SourceType.SqlServer => new SqlConnection(NormalizeSqlServerConnectionString(trimmed)),
            _ => throw new ArgumentOutOfRangeException(nameof(databaseType), databaseType, null)
        };
    }

    private static string NormalizeSqlServerConnectionString(string connectionString)
    {
        var builder = new SqlConnectionStringBuilder(connectionString)
        {
            MaxPoolSize = SqlProbeConnectionLimits.GisMetadataMaxPoolSize,
            ConnectTimeout = SqlProbeConnectionLimits.GisMetadataTimeoutSeconds
        };
        return builder.ConnectionString;
    }

    private static string FormatFailure(SourceType databaseType, Exception ex)
    {
        var message = ex switch
        {
            PostgresException { SqlState: PostgresErrorCodes.TooManyConnections } =>
                "PostgreSQL has no free connection slots (error 53300). " +
                "Close idle sessions or raise max_connections on the server; " +
                $"GIS probe pools are limited to {SqlProbeConnectionLimits.GisMetadataMaxPoolSize} connections per data source.",
            _ when ex.Message.Contains("remaining connection slots", StringComparison.OrdinalIgnoreCase) =>
                "PostgreSQL has no free connection slots (error 53300). " +
                "Close idle sessions or raise max_connections on the server; " +
                $"GIS probe pools are limited to {SqlProbeConnectionLimits.GisMetadataMaxPoolSize} connections per data source.",
            _ => ex.Message
        };

        return $"{databaseType.ToDisplayName()} connection failed: {message}";
    }

    private static void AddParameter(DbCommand command, string name, object value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value;
        command.Parameters.Add(parameter);
    }
}
