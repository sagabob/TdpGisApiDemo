using MongoDB.Bson;

namespace TdpGis.Infrastructure.Mongo;

public interface IGisMongoQueryRepository
{
    Task<List<BsonDocument>> SearchAsync(
        string connectionString,
        string databaseName,
        string collectionName,
        string queryField,
        string searchText,
        int maxResults,
        CancellationToken cancellationToken = default);
}