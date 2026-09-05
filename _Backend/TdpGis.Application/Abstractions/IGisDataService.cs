using System.Text.Json.Nodes;
using TdpGis.Domain;

namespace TdpGis.Application.Abstractions;

/// <summary>
///     GIS data access for configured entities. Phrase query by <see cref="GisConnection.QueryField"/> is
///     implemented today; additional query kinds (e.g. spatial) may be added on this or related contracts.
/// </summary>
public interface IGisDataService
{
    /// <summary>Phrase query: rows where the entity's configured query field matches <paramref name="searchText"/>.</summary>
    Task<List<JsonObject>> GetSearchedInstances(GisConnection gisConnection, string searchText,
        int maxResults, CancellationToken cancellationToken = default);
}