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
        string[] includeFields,
        int maxResults,
        CancellationToken cancellationToken = default);

    Task<PagedResult<Address>> SearchPagedAsync(string connectionString, string databaseName,
        string collectionName,
        string queryField, string searchText, string[] includeFields, int skip, int maxResults,
        CancellationToken cancellationToken = default);
}