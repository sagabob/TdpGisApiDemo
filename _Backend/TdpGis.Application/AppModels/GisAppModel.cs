using TdpGis.Domain;

namespace TdpGis.Application.AppModels;

public class GisAppModel
{
    public const int PageNumber = 50;

    public int PageLimit { get; set; } = PageNumber; //Temporarily set it in code, will be configurable

    public required Dictionary<string, GisConnection> QueryInstances { get; set; } = new();

    public List<GisConnectionDto> GetQueryConfigDto()
    {
        var queryConfigs = new List<GisConnectionDto>();

        queryConfigs.AddRange(QueryInstances.Select(x => new GisConnectionDto
        {
            Id = x.Value.Id,
            Name = x.Value.Name,
            Entity = x.Value.Entity,
            Description = x.Value.Description,
            QueryField = x.Value.QueryField,
            PropertyMappings = x.Value.PropertyMappings,
            GeometryType = x.Value.GeometryType,
            EntityLabel = x.Value.EntityLabel
        }));

        return queryConfigs;
    }

    public GisConnection? GetQueryInstance(string queryName)
    {
        QueryInstances.TryGetValue(queryName.ToLower(), out var inst);

        return inst;
    }
}
