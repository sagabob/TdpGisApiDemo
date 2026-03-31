using TdpGis.Application.AppModels;
using TdpGis.Models;

namespace TdpGis.Application.DatabaseService;

public interface IGisDbService
{
    Dictionary<string, GisConnection> QueryInstances { get; }

    List<GisConnectionDto> GetQueryConfigDto();

    GisConnection? GetQueryInstance(string queryName);
}