namespace TdpGis.AdminApplication.AppModels;

public sealed record DataSourceValidationApiResponse(
    bool Ok,
    string? Message,
    string? DatabaseName,
    IReadOnlyList<string>? Entities);

public sealed record CollectionsApiResponse(bool Ok, string? Message, IReadOnlyList<string>? Collections);

public sealed record DataSourceSampleApiResponse(
    bool Ok,
    string? Message,
    bool HasSample,
    IReadOnlyList<string> Fields,
    string SampleJson,
    string? SuggestedGeometryType = null);