namespace TdpGis.Application.Services;

public sealed record MongoValidationApiResponse(bool Ok, string? Message, string? DatabaseName,
    IReadOnlyList<string>? Collections);

public sealed record CollectionsApiResponse(bool Ok, string? Message, IReadOnlyList<string>? Collections);

public sealed record MongoSampleApiResponse(bool Ok, string? Message, bool HasSample, IReadOnlyList<string> Fields,
    string SampleJson);
