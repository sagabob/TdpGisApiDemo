using Microsoft.EntityFrameworkCore;
using TdpGis.Application.AppModels;
using TdpGis.Application.DatabaseService;
using TdpGis.ApplicationDb.Database;
using TdpGis.Models;

namespace TdpGis.ApplicationDb.DatabaseService;

public class GisDbService(GisAppDbContext dbContext) : IGisDbService
{
    public Dictionary<string, GisConnection> QueryInstances => 
        dbContext.GisConnections
            .AsNoTracking()
            .Include(x => x.PropertyMappings)
            .Include(x => x.DataSource)
            .ToDictionary(x => x.Name, x => x);

    public List<GisConnectionDto> GetQueryConfigDto()
    {
        return dbContext.GisConnections
            .AsNoTracking()
            .Select(x => new GisConnectionDto
            {
                Id = x.Id,
                Name = x.Name,
                GeometryType = x.GeometryType,
                QueryField = x.QueryField,
                PropertyMappings = x.PropertyMappings.ToList(),
                EntityLabel = x.EntityLabel,
                Description = x.Description
            })
            .ToList();
    }

    public GisConnection? GetQueryInstance(string queryName)
    {
        return dbContext.GisConnections
            .AsNoTracking()
            .Include(x => x.PropertyMappings)
            .Include(x => x.DataSource)
            .FirstOrDefault(x => x.Name == queryName);
    }
}