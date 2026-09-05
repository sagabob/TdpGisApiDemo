using System.Data.Common;
using Microsoft.Data.SqlClient;
using TdpGis.Domain;

namespace TdpGis.Infrastructure.Sql;

public sealed class GisSqlQueryRepository(PostgresDataSourceCache postgresDataSources) : IGisSqlQueryRepository
{
    public async Task<List<Dictionary<string, object?>>> SearchAsync(
        SourceType databaseType,
        string connectionString,
        string entity,
        string queryField,
        string searchText,
        IReadOnlyList<string> selectColumns,
        int maxResults,
        CancellationToken cancellationToken = default)
    {
        if (!databaseType.IsRelational())
            throw new NotSupportedException($"Database type '{databaseType}' is not a relational SQL source.");

        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        ArgumentException.ThrowIfNullOrWhiteSpace(entity);
        ArgumentException.ThrowIfNullOrWhiteSpace(queryField);
        ArgumentNullException.ThrowIfNull(selectColumns);

        var limit = Math.Clamp(maxResults, 1, 500);
        var (schema, table) = SqlIdentifiers.SplitTableName(entity, databaseType);
        await using var connection = await OpenConnectionAsync(databaseType, connectionString, cancellationToken);
        var columns = await LoadColumnsAsync(connection, databaseType, schema, table, cancellationToken);
        var columnByName = columns.ToDictionary(c => c.Name, StringComparer.OrdinalIgnoreCase);

        if (!columnByName.ContainsKey(queryField))
            throw new InvalidOperationException(
                $"Query field '{queryField}' was not found on {schema}.{table}.");

        var projected = selectColumns
            .Where(c => !string.IsNullOrWhiteSpace(c))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (projected.Count == 0)
            projected = columns.Select(c => c.Name).ToList();

        var missing = projected.Where(c => !columnByName.ContainsKey(c)).ToList();
        if (missing.Count > 0)
            throw new InvalidOperationException(
                $"Mapped column(s) not found on {schema}.{table}: {string.Join(", ", missing)}");

        var selectList = string.Join(", ", projected.Select(c =>
        {
            var quoted = SqlIdentifiers.Quote(databaseType, c);
            var dataType = columnByName[c].DataType;
            var expression = IsSpatialType(dataType)
                ? SpatialAsTextExpression(databaseType, quoted)
                : quoted;
            return $"{expression} AS {quoted}";
        }));

        var qualified =
            $"{SqlIdentifiers.Quote(databaseType, schema)}.{SqlIdentifiers.Quote(databaseType, table)}";
        var queryColumn = SqlIdentifiers.Quote(databaseType, queryField);
        var pattern = "%" + SqlIdentifiers.EscapeLikePattern(searchText) + "%";

        await using var command = connection.CreateCommand();
        if (databaseType == SourceType.SqlServer)
        {
            command.CommandText =
                $"""
                 SELECT TOP (@limit) {selectList}
                 FROM {qualified}
                 WHERE CONVERT(nvarchar(max), {queryColumn}) LIKE @pattern ESCAPE '\'
                 """;
            AddParameter(command, "@limit", limit);
            AddParameter(command, "@pattern", pattern);
        }
        else
        {
            command.CommandText =
                $"""
                 SELECT {selectList}
                 FROM {qualified}
                 WHERE CAST({queryColumn} AS text) ILIKE @pattern ESCAPE '\'
                 LIMIT @limit
                 """;
            AddParameter(command, "@pattern", pattern);
            AddParameter(command, "@limit", limit);
        }

        var rows = new List<Dictionary<string, object?>>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var row = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < reader.FieldCount; i++)
                row[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);

            rows.Add(row);
        }

        return rows;
    }

    private async Task<DbConnection> OpenConnectionAsync(
        SourceType databaseType,
        string connectionString,
        CancellationToken cancellationToken)
    {
        if (databaseType == SourceType.SqlServer)
        {
            var builder = new SqlConnectionStringBuilder(connectionString)
            {
                MaxPoolSize = SqlProbeConnectionLimits.GisMetadataMaxPoolSize,
                ConnectTimeout = SqlProbeConnectionLimits.GisMetadataTimeoutSeconds
            };
            var connection = new SqlConnection(builder.ConnectionString);
            await connection.OpenAsync(cancellationToken);
            return connection;
        }

        var pg = postgresDataSources.CreateConnection(connectionString);
        await pg.OpenAsync(cancellationToken);
        return pg;
    }

    private static async Task<List<(string Name, string DataType)>> LoadColumnsAsync(
        DbConnection connection,
        SourceType databaseType,
        string schema,
        string table,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        if (databaseType == SourceType.SqlServer)
        {
            command.CommandText =
                """
                SELECT c.COLUMN_NAME, c.DATA_TYPE
                FROM INFORMATION_SCHEMA.COLUMNS c
                WHERE c.TABLE_SCHEMA = @schema AND c.TABLE_NAME = @table
                ORDER BY c.ORDINAL_POSITION
                """;
            AddParameter(command, "@schema", schema);
            AddParameter(command, "@table", table);
        }
        else
        {
            command.CommandText =
                """
                SELECT c.column_name, c.udt_name
                FROM information_schema.columns c
                WHERE c.table_schema = @schema AND c.table_name = @table
                ORDER BY c.ordinal_position
                """;
            AddParameter(command, "@schema", schema);
            AddParameter(command, "@table", table);
        }

        var columns = new List<(string Name, string DataType)>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
            columns.Add((reader.GetString(0), reader.GetString(1)));

        return columns;
    }

    private static bool IsSpatialType(string dataType)
    {
        return dataType.Equals("geometry", StringComparison.OrdinalIgnoreCase) ||
               dataType.Equals("geography", StringComparison.OrdinalIgnoreCase);
    }

    private static string SpatialAsTextExpression(SourceType databaseType, string quotedColumn)
    {
        return databaseType == SourceType.SqlServer
            ? $"{quotedColumn}.STAsText()"
            : $"ST_AsText({quotedColumn})";
    }

    private static void AddParameter(DbCommand command, string name, object value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value;
        command.Parameters.Add(parameter);
    }
}