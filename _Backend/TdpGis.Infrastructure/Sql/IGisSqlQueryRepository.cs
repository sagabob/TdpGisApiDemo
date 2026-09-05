using TdpGis.Domain;

namespace TdpGis.Infrastructure.Sql;

public interface IGisSqlQueryRepository
{
    Task<List<Dictionary<string, object?>>> SearchAsync(
        SourceType databaseType,
        string connectionString,
        string entity,
        string queryField,
        string searchText,
        IReadOnlyList<string> selectColumns,
        int maxResults,
        CancellationToken cancellationToken = default);
}
