using TdpGis.AdminApplication.AppModels;
using TdpGis.Domain;

namespace TdpGis.AdminApplication.Services;

public interface IGisAdminAppService
{
    GisConfigurationPageData GetConfigurationPageData();

    GisConnection? GetGisConnectionById(Guid id);

    void MapGisConnectionToForm(GisConnectionFormState form, GisConnection connection);

    Task<FormActionResult> SaveMongoDataSourceAsync(string connectionString,
        CancellationToken cancellationToken = default);

    Task<FormActionResult> SaveGisConnectionAsync(SaveGisConnectionInput input,
        CancellationToken cancellationToken = default);

    Task<FormActionResult> AssignEntitiesToWorkspaceAsync(AssignEntitiesInput input,
        CancellationToken cancellationToken = default);

    Task<FormActionResult> SaveWorkspaceAsync(SaveWorkspaceInput input,
        CancellationToken cancellationToken = default);

    Task<FormActionResult> CreateWorkspaceAccessTokenAsync(CreateAccessTokenInput input,
        CancellationToken cancellationToken = default);

    Task<FormActionResult> UpdateWorkspaceAccessTokenAsync(UpdateAccessTokenInput input,
        CancellationToken cancellationToken = default);

    Task<MongoValidationApiResponse> ValidateMongoConnectionAsync(string connectionString,
        CancellationToken cancellationToken = default);

    Task<CollectionsApiResponse> GetCollectionsForDataSourceAsync(Guid dataSourceId,
        CancellationToken cancellationToken = default);

    Task<MongoSampleApiResponse> GetMongoSampleAsync(Guid dataSourceId, string collectionName,
        CancellationToken cancellationToken = default);
}

public sealed class GisConnectionFormState
{
    public Guid? GisConnectionId { get; set; }
    public Guid? DataSourceId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Entity { get; set; } = string.Empty;
    public string EntityLabel { get; set; } = string.Empty;
    public string QueryField { get; set; } = string.Empty;
    public GeometryType GeometryType { get; set; } = GeometryType.MultiPolygon;
    public Guid? GisWorkspaceId { get; set; }
    public string PropertyMappingsText { get; set; } = string.Empty;
}

public sealed record SaveGisConnectionInput(
    Guid? GisConnectionId,
    Guid? DataSourceId,
    string Name,
    string? Description,
    string Entity,
    string EntityLabel,
    string QueryField,
    GeometryType GeometryType,
    Guid? GisWorkspaceId,
    string PropertyMappingsText);

public sealed record AssignEntitiesInput(Guid WorkspaceId, IReadOnlyList<Guid> SelectedConnectionIds);

public sealed record SaveWorkspaceInput(Guid? WorkspaceId, string Name);

public sealed record CreateAccessTokenInput(
    Guid WorkspaceId,
    string Name,
    DateTime ExpiredDateTime,
    bool IsActive,
    bool IsPublic);

public sealed record UpdateAccessTokenInput(
    Guid TokenId,
    Guid GisWorkspaceId,
    string Name,
    DateTime ExpiredDateTime,
    bool IsActive,
    bool IsPublic);