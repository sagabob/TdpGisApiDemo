using Microsoft.EntityFrameworkCore;
using TdpGis.Models;

namespace TdpGis.Application.Database;

public interface IGisDbContext
{
    DbSet<DataSourceSetting> DataSourceSettings { get; set; }

    DbSet<GisConnection> GisConnections { get; set; }

    DbSet<PropertyMapping> PropertyMappings { get; set; }
}