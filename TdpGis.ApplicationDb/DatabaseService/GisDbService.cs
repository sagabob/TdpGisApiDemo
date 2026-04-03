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

    public List<GisConnection> GetAllConnections()
    {
        return dbContext.GisConnections
            .AsNoTracking()
            .Include(x => x.PropertyMappings)
            .Include(x => x.DataSource)
            .OrderBy(x => x.Name)
            .ToList();
    }

    public async Task<GisConnection> CreateConnectionAsync(GisConnection connection, CancellationToken cancellationToken = default)
    {
        // Reuse existing DataSourceSetting row instead of inserting duplicate key.
        dbContext.Attach(connection.DataSource);
        var entity = await dbContext.GisConnections.AddAsync(connection, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        return entity.Entity;
    }

    public List<DataSourceSetting> GetMongoDataSources()
    {
        return dbContext.DataSourceSettings
            .AsNoTracking()
            .Where(x => x.DatabaseType == SourceType.Mongodb)
            .OrderBy(x => x.ConnectionString)
            .ToList();
    }

    public DataSourceSetting? GetDataSourceById(Guid id)
    {
        return dbContext.DataSourceSettings
            .AsNoTracking()
            .FirstOrDefault(x => x.Id == id);
    }

    public async Task<DataSourceSetting> CreateMongoDataSourceAsync(string connectionString, CancellationToken cancellationToken = default)
    {
        var normalized = connectionString.Trim();
        var existing = dbContext.DataSourceSettings
            .FirstOrDefault(x => x.DatabaseType == SourceType.Mongodb && x.ConnectionString == normalized);
        if (existing is not null)
        {
            return existing;
        }

        var ds = new DataSourceSetting
        {
            Id = Guid.NewGuid(),
            ConnectionString = normalized,
            DatabaseType = SourceType.Mongodb
        };
        var entity = await dbContext.DataSourceSettings.AddAsync(ds, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        return entity.Entity;
    }
}