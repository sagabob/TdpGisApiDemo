using System.Text.Json.Nodes;
using TdpGis.AdminApplication.Abstractions;
using TdpGis.Domain;
using TdpGis.Infrastructure.Helpers;

namespace TdpGis.Infrastructure.Mongo;

public class GisMongoDataService(
    IMongoMetadataProvider mongoMetadataProvider,
    IGisMongoQueryRepository mongoQueryRepository)
{
    public async Task<List<JsonObject>> GetSearchedInstances(
        GisConnection gisConnection,
        string searchText,
        int maxResults,
        CancellationToken cancellationToken = default)
    {
        var connectionString = gisConnection.DataSource.ConnectionString.Trim();
        var resolvedDatabaseName = mongoMetadataProvider.GetDatabaseName(connectionString);
        var rows = new List<JsonObject>();
        var bsonResults = await mongoQueryRepository.SearchAsync(
            connectionString,
            resolvedDatabaseName,
            gisConnection.Entity,
            gisConnection.QueryField,
            searchText,
            maxResults,
            cancellationToken);

        bsonResults.ForEach(x => rows.Add(OutputMapping.ConvertFromBson(x, gisConnection.PropertyMappings)));

        return rows;
    }
}