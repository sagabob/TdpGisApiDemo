using TdpGis.LocalApi.Models;

namespace TdpGis.LocalApi.DatabaseServices;

public interface IGisMongoQueryRepository
{
    Task<List<Address>> SearchAsync(
        string connectionString,
        string databaseName,
        string collectionName,
        string queryField,
        string searchText,
        int maxResults,
        CancellationToken cancellationToken = default);
}