namespace TdpGis.AdminApplication.Abstractions;

public interface IMongoMetadataProvider
{
    Task<MongoConnectionProbeResult> ProbeConnectionAsync(string connectionString, string? collectionName,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<string>> ListCollectionNamesAsync(string connectionString,
        CancellationToken cancellationToken = default);

    Task<MongoDocumentSampleResult> GetSampleDocumentAsync(string connectionString, string collectionName,
        CancellationToken cancellationToken = default);
}

public sealed record MongoConnectionProbeResult(
    bool IsValid,
    string ErrorMessage,
    string? DatabaseName,
    IReadOnlyList<string>? Collections);

public sealed record MongoDocumentSampleResult(bool HasSample, IReadOnlyList<string> Fields, string SampleJson);