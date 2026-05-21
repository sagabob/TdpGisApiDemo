using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using TdpGis.LocalApi.Models;

namespace TdpGis.LocalApi.DatabaseServices;

public sealed class GisMongoQueryRepository : IGisMongoQueryRepository
{
    // Example: return typed Address objects and only requested fields (excludes _id by default)
    public async Task<List<Address>> SearchAsync(string connectionString, string databaseName, string collectionName,
        string queryField, string searchText, string[] includeFields, int maxResults,
        CancellationToken cancellationToken)
    {
        var client = new MongoClient(connectionString);
        var db = client.GetDatabase(databaseName);
        var col = db.GetCollection<BsonDocument>(collectionName);

        var filter = Builders<BsonDocument>.Filter.Regex(queryField, new BsonRegularExpression(searchText, "i"));

        var projectionBuilder = Builders<BsonDocument>.Projection;
        var proj = projectionBuilder.Exclude("_id");

        foreach (var f in includeFields)
            proj = proj.Include(f);

        var docs = await col.Find(filter)
            .Project(proj)
            .Limit(maxResults)
            .ToListAsync(cancellationToken);

        var output = docs.Select(d => BsonSerializer.Deserialize<Address>(d));

        return output.ToList();
    }

    public async Task<PagedResult<Address>> SearchPagedAsync(string connectionString, string databaseName,
        string collectionName,
        string queryField, string searchText, string[] includeFields, int skip, int maxResults,
        CancellationToken cancellationToken)
    {
        var client = new MongoClient(connectionString);
        var db = client.GetDatabase(databaseName);
        var col = db.GetCollection<BsonDocument>(collectionName);

        var filter = Builders<BsonDocument>.Filter.Regex(queryField, new BsonRegularExpression(searchText, "i"));

        var total = await col.CountDocumentsAsync(filter, cancellationToken: cancellationToken);


        var projectionBuilder = Builders<BsonDocument>.Projection;
        var proj = projectionBuilder.Exclude("_id");

        foreach (var f in includeFields)
            proj = proj.Include(f);

        var docs = await col.Find(filter)
            .Project(proj).Skip(skip - 1)
            .Limit(maxResults)
            .ToListAsync(cancellationToken);

        var output = docs.Select(d => BsonSerializer.Deserialize<Address>(d)).ToList();

        return new PagedResult<Address>
        {
            TotalCount = total,
            Page = skip,
            PageSize = maxResults,
            Items = output
        };
        ;
    }
}