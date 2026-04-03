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

    Task<DataSourceSetting> CreateMongoDataSourceAsync(string connectionString, CancellationToken cancellationToken = default);
}