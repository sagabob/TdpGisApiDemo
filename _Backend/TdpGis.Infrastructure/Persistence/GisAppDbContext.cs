using Microsoft.EntityFrameworkCore;
using TdpGis.Domain;

namespace TdpGis.Infrastructure.Persistence;

public class GisAppDbContext(DbContextOptions<GisAppDbContext> options) : DbContext(options)
{
    public DbSet<DataSourceSetting> DataSourceSettings { get; set; }

    public DbSet<GisConnection> GisConnections { get; set; }

    public DbSet<PropertyMapping> PropertyMappings { get; set; }

    public DbSet<GisWorkspace> GisWorkspaces { get; set; }

    public DbSet<GisWorkspaceAccessToken> GisWorkspaceAccessTokens { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(GisAppDbContext).Assembly);
    }
}