using System.Text.RegularExpressions;
using MongoDB.Bson;
using MongoDB.Driver;

namespace TdpGis.Infrastructure.Mongo;

public sealed class GisMongoQueryRepository(MongoClientCache mongoClients) : IGisMongoQueryRepository
{
    public async Task<List<BsonDocument>> SearchAsync(
        string connectionString,
        string databaseName,
        string collectionName,
        string queryField,
        string searchText,
        int maxResults,
        CancellationToken cancellationToken = default)
    {
        var client = mongoClients.GetOrCreate(connectionString);
        var database = client.GetDatabase(databaseName);
        var collection = database.GetCollection<BsonDocument>(collectionName);

        var queryExpr = new BsonRegularExpression(new Regex(searchText, RegexOptions.IgnoreCase));
        var filterByText = Builders<BsonDocument>.Filter.Regex(queryField, queryExpr);
        return await collection.Find(filterByText).Limit(maxResults).ToListAsync(cancellationToken);
    }
}
