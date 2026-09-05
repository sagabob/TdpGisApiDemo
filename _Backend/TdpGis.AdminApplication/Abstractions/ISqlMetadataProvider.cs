using TdpGis.Domain;

namespace TdpGis.AdminApplication.Abstractions;

public interface ISqlMetadataProvider
{
    Task<SqlConnectionProbeResult> ProbeConnectionAsync(SourceType databaseType, string connectionString,
        string? tableName, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<string>> ListTableNamesAsync(SourceType databaseType, string connectionString,
        CancellationToken cancellationToken = default);

    Task<SqlRowSampleResult> GetSampleRowAsync(SourceType databaseType, string connectionString, string tableName,
        CancellationToken cancellationToken = default);
}

public sealed record SqlConnectionProbeResult(
    bool IsValid,
    string ErrorMessage,
    string? DatabaseName,
    IReadOnlyList<string>? Tables);

public sealed record SqlRowSampleResult(bool HasSample, IReadOnlyList<string> Fields, string SampleJson);