using System.Text.Json.Nodes;
using TdpGis.Application.Abstractions;
using TdpGis.Domain;
using TdpGis.Infrastructure.Mongo;
using TdpGis.Infrastructure.Sql;

namespace TdpGis.Infrastructure;

/// <summary>
///     Routes GIS entity search to MongoDB or relational (Postgres / SQL Server) implementations.
/// </summary>
public sealed class GisDataService(
    GisMongoDataService mongoDataService,
    GisSqlDataService sqlDataService) : IGisDataService
{
    public Task<List<JsonObject>> GetSearchedInstances(
        GisConnection gisConnection,
        string searchText,
        int maxResults,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(gisConnection);
        ArgumentNullException.ThrowIfNull(gisConnection.DataSource);

        return gisConnection.DataSource.DatabaseType switch
        {
            SourceType.Mongodb => mongoDataService.GetSearchedInstances(
                gisConnection, searchText, maxResults, cancellationToken),
            SourceType.Postgres or SourceType.SqlServer => sqlDataService.GetSearchedInstances(
                gisConnection, searchText, maxResults, cancellationToken),
            _ => throw new NotSupportedException(
                $"Unsupported GIS data source type '{gisConnection.DataSource.DatabaseType}'.")
        };
    }
}
