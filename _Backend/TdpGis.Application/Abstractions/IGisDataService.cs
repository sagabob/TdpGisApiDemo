using System.Text.Json.Nodes;
using TdpGis.Domain;

namespace TdpGis.Application.Abstractions;

public interface IGisDataService
{
    Task<List<JsonObject>> GetSearchedInstances(GisConnection gisConnection, string searchText,
        int maxResults, CancellationToken cancellationToken = default);
}