using System.Text.RegularExpressions;
using MongoDB.Bson;
using MongoDB.Driver;
using Newtonsoft.Json.Linq;
using TdpGis.Application.Abstractions;
using TdpGis.Domain;
using TdpGis.Infrastructure.Helpers;

namespace TdpGis.Infrastructure.Mongo;

public class GisMongoDataService : IGisDataService
{
    public async Task<List<JObject>> GetSearchedInstances(GisConnection gisConnection, string searchText,
        int maxResults, CancellationToken cancellationToken = default)
    {
        var connectionString = gisConnection.DataSource.ConnectionString.Trim();
        var resolvedDatabaseName = GetDatabaseName(connectionString);
        var client = new MongoClient(connectionString);
        var database = client.GetDatabase(resolvedDatabaseName);

        var collection = database.GetCollection<BsonDocument>(gisConnection.Entity);

        var jObjects = new List<JObject>();

        if (collection == null) return jObjects;

        var queryExpr = new BsonRegularExpression(new Regex(searchText, RegexOptions.IgnoreCase));

        var filterByText = Builders<BsonDocument>.Filter.Regex(gisConnection.QueryField, queryExpr);

        var bsonResults = await collection.Find(filterByText).Limit(maxResults).ToListAsync(cancellationToken);

        bsonResults.ForEach(x => jObjects.Add(OutputMapping.ConvertFromBson(x, gisConnection.PropertyMappings)));

        return jObjects;
    }

    private static string GetDatabaseName(string connectionString)
    {
        var mongoUrl = MongoUrl.Create(connectionString.Trim());
        return mongoUrl.DatabaseName ?? string.Empty;
    }
}