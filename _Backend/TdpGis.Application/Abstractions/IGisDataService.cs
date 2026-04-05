using Newtonsoft.Json.Linq;
using TdpGis.Domain;

namespace TdpGis.Application.Abstractions;

public interface IGisDataService
{
    Task<List<JObject>> GetSearchedInstances(GisConnection gisConnection, string searchText,
        int maxResults, CancellationToken cancellationToken = default);
}