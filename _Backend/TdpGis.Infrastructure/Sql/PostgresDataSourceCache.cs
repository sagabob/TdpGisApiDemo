using System.Collections.Concurrent;
using Npgsql;

namespace TdpGis.Infrastructure.Sql;

/// <summary>
///     Shares <see cref="NpgsqlDataSource" /> instances per GIS connection string and caps pool size so
///     admin probes do not exhaust small hosted Postgres limits (e.g. Aiven error 53300).
/// </summary>
public sealed class PostgresDataSourceCache : IDisposable
{
    private readonly ConcurrentDictionary<string, NpgsqlDataSource> _sources = new(StringComparer.Ordinal);
    private bool _disposed;

    public NpgsqlConnection CreateConnection(string connectionString)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var normalized = NormalizeForPooling(connectionString);
        var dataSource = _sources.GetOrAdd(normalized, static cs => NpgsqlDataSource.Create(cs));
        return dataSource.CreateConnection();
    }

    public static string NormalizeForPooling(string connectionString)
    {
        var builder = new NpgsqlConnectionStringBuilder(connectionString.Trim())
        {
            MaxPoolSize = SqlProbeConnectionLimits.GisMetadataMaxPoolSize,
            Timeout = SqlProbeConnectionLimits.GisMetadataTimeoutSeconds
        };
        return builder.ConnectionString;
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        foreach (var source in _sources.Values)
            source.Dispose();
        _sources.Clear();
    }
}
