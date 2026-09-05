using System.Text.Json.Nodes;
using TdpGis.Domain;
using TdpGis.Infrastructure.Helpers;

namespace TdpGis.Infrastructure.Sql;

public sealed class GisSqlDataService(IGisSqlQueryRepository queryRepository)
{
    public async Task<List<JsonObject>> GetSearchedInstances(
        GisConnection gisConnection,
        string searchText,
        int maxResults,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(gisConnection);
        ArgumentNullException.ThrowIfNull(gisConnection.DataSource);

        var databaseType = gisConnection.DataSource.DatabaseType;
        if (!databaseType.IsRelational())
            throw new NotSupportedException(
                $"GisSqlDataService does not support database type '{databaseType}'.");

        var selectColumns = gisConnection.PropertyMappings
            .Where(m => !string.IsNullOrWhiteSpace(m.PropertyName))
            .Select(m => m.PropertyName)
            .ToList();

        var rows = await queryRepository.SearchAsync(
            databaseType,
            gisConnection.DataSource.ConnectionString,
            gisConnection.Entity,
            gisConnection.QueryField,
            searchText,
            selectColumns,
            maxResults,
            cancellationToken);

        return rows
            .Select(row => OutputMapping.ConvertFromRow(row, gisConnection.PropertyMappings))
            .ToList();
    }
}