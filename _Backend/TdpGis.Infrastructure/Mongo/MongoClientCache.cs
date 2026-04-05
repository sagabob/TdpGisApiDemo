using System.Collections.Concurrent;
using MongoDB.Driver;

namespace TdpGis.Infrastructure.Mongo;

/// <summary>
///     Reuses <see cref="MongoClient" /> instances per connection string. The driver is designed to be long-lived;
///     creating a new client per request adds overhead and extra pools.
/// </summary>
public sealed class MongoClientCache
{
    private readonly ConcurrentDictionary<string, MongoClient> _clients = new(StringComparer.Ordinal);

    public MongoClient GetOrCreate(string connectionString)
    {
        var key = connectionString.Trim();
        return _clients.GetOrAdd(key, static cs => new MongoClient(cs));
    }
}
