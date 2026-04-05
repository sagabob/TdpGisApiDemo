using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Driver;
using TdpGis.AdminApplication.Abstractions;

namespace TdpGis.Infrastructure.Mongo;

public sealed class MongoMetadataProvider : IMongoMetadataProvider
{
    public async Task<MongoConnectionProbeResult> ProbeConnectionAsync(string connectionString, string? collectionName,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            return new MongoConnectionProbeResult(false, "MongoDB connection string is required.", null, null);

        var resolvedDatabaseName = GetDatabaseName(connectionString);
        if (string.IsNullOrWhiteSpace(resolvedDatabaseName))
            return new MongoConnectionProbeResult(false,
                "Database name is required in the MongoDB connection string (mongodb://.../<database>).", null, null);

        try
        {
            var client = new MongoClient(connectionString.Trim());
            var database = client.GetDatabase(resolvedDatabaseName);
            var collections =
                await (await database.ListCollectionNamesAsync(cancellationToken: cancellationToken)).ToListAsync(
                    cancellationToken);
            if (!string.IsNullOrWhiteSpace(collectionName) &&
                !collections.Contains(collectionName.Trim(), StringComparer.Ordinal))
                return new MongoConnectionProbeResult(false,
                    $"Collection '{collectionName}' was not found in database '{resolvedDatabaseName}'.", null, null);

            return new MongoConnectionProbeResult(true, string.Empty, resolvedDatabaseName, collections);
        }
        catch (Exception ex)
        {
            return new MongoConnectionProbeResult(false, $"MongoDB connection failed: {ex.Message}", null, null);
        }
    }

    public async Task<IReadOnlyList<string>> ListCollectionNamesAsync(string connectionString,
        CancellationToken cancellationToken = default)
    {
        var databaseName = GetDatabaseName(connectionString);
        var client = new MongoClient(connectionString.Trim());
        var database = client.GetDatabase(databaseName);
        return await (await database.ListCollectionNamesAsync(cancellationToken: cancellationToken)).ToListAsync(
            cancellationToken);
    }

    public async Task<MongoDocumentSampleResult> GetSampleDocumentAsync(string connectionString, string collectionName,
        CancellationToken cancellationToken = default)
    {
        var databaseName = GetDatabaseName(connectionString);
        var client = new MongoClient(connectionString.Trim());
        var database = client.GetDatabase(databaseName);
        var collection = database.GetCollection<BsonDocument>(collectionName.Trim());
        var sample = await collection.Find(FilterDefinition<BsonDocument>.Empty).Limit(1)
            .FirstOrDefaultAsync(cancellationToken);

        if (sample is null)
            return new MongoDocumentSampleResult(false, [], "{}");

        var fields = sample.Names.ToArray();
        var json = sample.ToJson(new JsonWriterSettings { Indent = true });
        return new MongoDocumentSampleResult(true, fields, json);
    }

    private static string GetDatabaseName(string connectionString)
    {
        var mongoUrl = MongoUrl.Create(connectionString.Trim());
        return mongoUrl.DatabaseName ?? string.Empty;
    }
}