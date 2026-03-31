using Microsoft.EntityFrameworkCore;
using TdpGis.Models;

namespace TdpGis.ApplicationDb.Database;

public class GisAppDbContext(DbContextOptions<GisAppDbContext> options) : DbContext(options)
{
    public DbSet<DataSourceSetting> DataSourceSettings { get; set; }

    public DbSet<GisConnection> GisConnections { get; set; }

    public DbSet<PropertyMapping> PropertyMappings { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(GisAppDbContext).Assembly);
    }
}