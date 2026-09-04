using TdpGis.Domain;

namespace TdpGis.AdminApplication.Abstractions;

public interface IGisConfigurationRepository
{
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

    List<DataSourceSetting> GetDataSources(SourceType? databaseType = null);

    List<DataSourceSetting> GetMongoDataSources();

    DataSourceSetting? GetDataSourceById(Guid id);

    Task<DataSourceSetting> CreateDataSourceAsync(SourceType databaseType, string connectionString,
        CancellationToken cancellationToken = default);

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