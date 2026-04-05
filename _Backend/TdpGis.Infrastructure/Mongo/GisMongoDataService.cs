using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using MongoDB.Bson;
using MongoDB.Driver;
using TdpGis.AdminApplication.Abstractions;
using TdpGis.Application.Abstractions;
using TdpGis.Domain;
using TdpGis.Infrastructure.Helpers;

namespace TdpGis.Infrastructure.Mongo;

public class GisMongoDataService(IMongoMetadataProvider mongoMetadataProvider) : IGisDataService
{
    public async Task<List<JsonObject>> GetSearchedInstances(GisConnection gisConnection, string searchText,
        int maxResults, CancellationToken cancellationToken = default)
    {
        var connectionString = gisConnection.DataSource.ConnectionString.Trim();
        var resolvedDatabaseName = mongoMetadataProvider.GetDatabaseName(connectionString);
        var client = new MongoClient(connectionString);
        var database = client.GetDatabase(resolvedDatabaseName);

        var collection = database.GetCollection<BsonDocument>(gisConnection.Entity);

        var rows = new List<JsonObject>();

        if (collection == null) return rows;

        var queryExpr = new BsonRegularExpression(new Regex(searchText, RegexOptions.IgnoreCase));

        var filterByText = Builders<BsonDocument>.Filter.Regex(gisConnection.QueryField, queryExpr);

        var bsonResults = await collection.Find(filterByText).Limit(maxResults).ToListAsync(cancellationToken);

        bsonResults.ForEach(x => rows.Add(OutputMapping.ConvertFromBson(x, gisConnection.PropertyMappings)));

        return rows;
    }
}