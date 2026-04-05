using TdpGis.Application.AppModels;
using TdpGis.Domain;

namespace TdpGis.Application.Abstractions;

public interface IGisConfigurationRepository
{
    /// <summary>
    ///     GIS connections assigned to the workspace, as public DTOs (no connection strings).
    /// </summary>
    List<GisConnectionDto> GetGisConnectionDtoByWorkspaceId(Guid workspaceId);

    /// <summary>
    ///     Returns the access token row if it matches the workspace, secret, is active, and not expired.
    /// </summary>
    Task<GisWorkspaceAccessToken?> GetValidWorkspaceAccessTokenAsync(
        Guid workspaceId,
        string accessToken,
        CancellationToken cancellationToken = default);

    GisConnection? GetQueryInstance(string queryName);

    List<GisConnection> GetAllConnections();

    GisConnection? GetConnectionById(Guid id);

    bool GisConnectionNameExists(string name, Guid? excludeConnectionId = null);

    Task<GisConnection> CreateConnectionAsync(GisConnection connection, CancellationToken cancellationToken = default);

    Task<GisConnection?> UpdateConnectionAsync(
        Guid id,
        Guid dataSourceId,
        string name,
        string description,
        string entity,
        string entityLabel,
        string queryField,
        GeometryType geometryType,
        Guid? gisWorkspaceId,
        List<PropertyMapping> propertyMappings,
        CancellationToken cancellationToken = default);

    List<DataSourceSetting> GetMongoDataSources();

    DataSourceSetting? GetDataSourceById(Guid id);

    Task<DataSourceSetting> CreateMongoDataSourceAsync(string connectionString,
        CancellationToken cancellationToken = default);

    List<GisWorkspace> GetAllWorkspaces();

    GisWorkspace? GetWorkspaceById(Guid id);

    Task<GisWorkspace> CreateWorkspaceAsync(string name, CancellationToken cancellationToken = default);

    Task<GisWorkspace?> UpdateWorkspaceAsync(Guid id, string name, CancellationToken cancellationToken = default);

    Task<GisWorkspaceAccessToken> CreateWorkspaceAccessTokenAsync(
        Guid workspaceId,
        string name,
        DateTime expiredDateTime,
        bool isActive,
        bool isPublic,
        CancellationToken cancellationToken = default);

    Task<GisWorkspaceAccessToken?> UpdateWorkspaceAccessTokenAsync(
        Guid tokenId,
        Guid gisWorkspaceId,
        string name,
        DateTime expiredDateTime,
        bool isActive,
        bool isPublic,
        CancellationToken cancellationToken = default);

    Task<int> SetConnectionsWorkspaceAsync(
        Guid workspaceId,
        IReadOnlyList<Guid> connectionIds,
        CancellationToken cancellationToken = default);
}
