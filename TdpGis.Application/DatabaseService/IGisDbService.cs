using TdpGis.Application.AppModels;
using TdpGis.Models;

namespace TdpGis.Application.DatabaseService;

public interface IGisDbService
{
    Dictionary<string, GisConnection> QueryInstances { get; }

    List<GisConnectionDto> GetQueryConfigDto();

    GisConnection? GetQueryInstance(string queryName);

    List<GisConnection> GetAllConnections();

    Task<GisConnection> CreateConnectionAsync(GisConnection connection, CancellationToken cancellationToken = default);

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