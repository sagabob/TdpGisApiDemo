using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using TdpGis.AdminApplication.Abstractions;
using TdpGis.Domain;
using TdpGis.Infrastructure.Helpers;

namespace TdpGis.Infrastructure.Persistence;

public class GisConfigurationRepository(GisAppDbContext dbContext) : IGisConfigurationRepository
{
    public List<GisConnection> GetAllConnections()
    {
        return dbContext.GisConnections
            .AsNoTracking()
            .Include(x => x.PropertyMappings)
            .Include(x => x.DataSource)
            .Include(x => x.GisWorkspace)
            .OrderBy(x => x.Name)
            .ToList();
    }

    public GisConnection? GetConnectionById(Guid id)
    {
        return dbContext.GisConnections
            .AsNoTracking()
            .Include(x => x.PropertyMappings)
            .Include(x => x.DataSource)
            .Include(x => x.GisWorkspace)
            .FirstOrDefault(x => x.Id == id);
    }

    public bool GisConnectionNameExists(string name, Guid? excludeConnectionId = null)
    {
        var trimmed = name.Trim();
        return dbContext.GisConnections
            .AsNoTracking()
            .Any(c => c.Name == trimmed && (!excludeConnectionId.HasValue || c.Id != excludeConnectionId.Value));
    }

    public async Task<GisConnection> CreateConnectionAsync(GisConnection connection,
        CancellationToken cancellationToken = default)
    {
        // Reuse existing DataSourceSetting row instead of inserting duplicate key.
        dbContext.Attach(connection.DataSource);
        var entity = await dbContext.GisConnections.AddAsync(connection, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        return entity.Entity;
    }

    public async Task<GisConnection?> UpdateConnectionAsync(
        Guid id,
        Guid dataSourceId,
        string name,
        string description,
        string entity,
        string entityLabel,
        string queryField,
        GeometryType geometryType,
        Guid? gisWorkspaceId,
        List<PropertyMapping> propertyMappings,
        CancellationToken cancellationToken = default)
    {
        var nameTrimmed = name.Trim();
        var descriptionTrimmed = description.Trim();
        var entityTrimmed = entity.Trim();
        var entityLabelTrimmed = entityLabel.Trim();
        var queryFieldTrimmed = queryField.Trim();

        var connectionExists = await dbContext.GisConnections.AnyAsync(c => c.Id == id, cancellationToken);
        if (!connectionExists) return null;

        var dataSourceExists =
            await dbContext.DataSourceSettings.AnyAsync(d => d.Id == dataSourceId, cancellationToken);
        if (!dataSourceExists) throw new InvalidOperationException("Selected data source connection was not found.");

        // Avoid loading a tracked graph: tracked DELETE/UPDATE + SaveChanges can report 0 rows affected
        // (DbUpdateConcurrencyException). Use bulk ExecuteDelete/ExecuteUpdate, then INSERT new mappings
        // in one transaction; SaveChanges only inserts PropertyMappings.
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        await dbContext.PropertyMappings
            .Where(p => EF.Property<Guid>(p, "GisConnectionId") == id)
            .ExecuteDeleteAsync(cancellationToken);

        var rowsUpdated = await dbContext.GisConnections
            .Where(c => c.Id == id)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(c => c.Name, nameTrimmed)
                    .SetProperty(c => c.Description, descriptionTrimmed)
                    .SetProperty(c => c.Entity, entityTrimmed)
                    .SetProperty(c => c.EntityLabel, entityLabelTrimmed)
                    .SetProperty(c => c.QueryField, queryFieldTrimmed)
                    .SetProperty(c => c.GeometryType, geometryType)
                    .SetProperty(c => c.GisWorkspaceId, gisWorkspaceId)
                    .SetProperty(c => c.DataSourceId, dataSourceId),
                cancellationToken);

        if (rowsUpdated != 1)
        {
            await transaction.RollbackAsync(cancellationToken);
            return null;
        }

        foreach (var pm in propertyMappings)
        {
            var row = new PropertyMapping
            {
                Id = Guid.NewGuid(),
                PropertyName = pm.PropertyName,
                PropertyLabel = pm.PropertyLabel,
                ColumnType = pm.ColumnType
            };
            dbContext.PropertyMappings.Add(row);
            dbContext.Entry(row).Property("GisConnectionId").CurrentValue = id;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return GetConnectionById(id);
    }

    public List<DataSourceSetting> GetDataSources(SourceType? databaseType = null)
    {
        var query = dbContext.DataSourceSettings.AsNoTracking();
        if (databaseType.HasValue)
            query = query.Where(x => x.DatabaseType == databaseType.Value);

        return query
            .OrderBy(x => x.DatabaseType)
            .ThenBy(x => x.Name)
            .ThenBy(x => x.ConnectionString)
            .ToList();
    }

    public List<DataSourceSetting> GetMongoDataSources()
    {
        return GetDataSources(SourceType.Mongodb);
    }

    public DataSourceSetting? GetDataSourceById(Guid id)
    {
        return dbContext.DataSourceSettings
            .AsNoTracking()
            .FirstOrDefault(x => x.Id == id);
    }

    public async Task<DataSourceSetting> CreateDataSourceAsync(SourceType databaseType, string name,
        string connectionString, CancellationToken cancellationToken = default)
    {
        var normalizedName = name.Trim();
        var normalized = connectionString.Trim();
        var existing = dbContext.DataSourceSettings
            .FirstOrDefault(x => x.DatabaseType == databaseType && x.ConnectionString == normalized);
        if (existing is not null)
        {
            if (!string.Equals(existing.Name, normalizedName, StringComparison.Ordinal))
            {
                existing.Name = normalizedName;
                await dbContext.SaveChangesAsync(cancellationToken);
            }

            return existing;
        }

        var ds = new DataSourceSetting
        {
            Id = Guid.NewGuid(),
            Name = normalizedName,
            ConnectionString = normalized,
            DatabaseType = databaseType
        };
        var entity = await dbContext.DataSourceSettings.AddAsync(ds, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        return entity.Entity;
    }

    public Task<DataSourceSetting> CreateMongoDataSourceAsync(string name, string connectionString,
        CancellationToken cancellationToken = default)
    {
        return CreateDataSourceAsync(SourceType.Mongodb, name, connectionString, cancellationToken);
    }

    public List<GisWorkspace> GetAllWorkspaces()
    {
        return dbContext.GisWorkspaces
            .AsNoTracking()
            .Include(w => w.Entities)
            .Include(w => w.AccessTokens)
            .OrderBy(w => w.Name)
            .ToList();
    }

    public GisWorkspace? GetWorkspaceById(Guid id)
    {
        return dbContext.GisWorkspaces
            .AsNoTracking()
            .Include(w => w.Entities)
            .Include(w => w.AccessTokens)
            .FirstOrDefault(w => w.Id == id);
    }

    public async Task<GisWorkspace> CreateWorkspaceAsync(string name, CancellationToken cancellationToken = default)
    {
        var workspace = new GisWorkspace
        {
            Id = Guid.NewGuid(),
            Name = name.Trim()
        };
        await dbContext.GisWorkspaces.AddAsync(workspace, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        return workspace;
    }

    public async Task<GisWorkspace?> UpdateWorkspaceAsync(Guid id, string name,
        CancellationToken cancellationToken = default)
    {
        var workspace = await dbContext.GisWorkspaces.FirstOrDefaultAsync(w => w.Id == id, cancellationToken);
        if (workspace is null) return null;

        workspace.Name = name.Trim();
        await dbContext.SaveChangesAsync(cancellationToken);
        return workspace;
    }

    public async Task<GisWorkspaceAccessToken> CreateWorkspaceAccessTokenAsync(
        Guid workspaceId,
        string name,
        DateTime expiredDateTime,
        bool isActive,
        bool isPublic,
        CancellationToken cancellationToken = default)
    {
        var workspace = await dbContext.GisWorkspaces.FirstOrDefaultAsync(w => w.Id == workspaceId, cancellationToken);
        if (workspace is null) throw new InvalidOperationException("Workspace was not found.");

        var token = new GisWorkspaceAccessToken
        {
            Id = Guid.NewGuid(),
            GisWorkspaceId = workspaceId,
            Name = name.Trim(),
            AccessToken = GenerateOpaqueToken(),
            ExpiredDateTime = DateTimeUtcForPostgreSql.ToUtc(expiredDateTime),
            IsActive = isActive,
            IsPublic = isPublic,
            GisWorkspace = workspace
        };

        await dbContext.GisWorkspaceAccessTokens.AddAsync(token, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        return token;
    }

    public async Task<GisWorkspaceAccessToken?> UpdateWorkspaceAccessTokenAsync(
        Guid tokenId,
        Guid gisWorkspaceId,
        string name,
        DateTime expiredDateTime,
        bool isActive,
        bool isPublic,
        CancellationToken cancellationToken = default)
    {
        var token = await dbContext.GisWorkspaceAccessTokens
            .FirstOrDefaultAsync(t => t.Id == tokenId, cancellationToken);
        if (token is null) return null;

        var workspaceExists = await dbContext.GisWorkspaces.AnyAsync(w => w.Id == gisWorkspaceId, cancellationToken);
        if (!workspaceExists) throw new InvalidOperationException("Workspace was not found.");

        token.GisWorkspaceId = gisWorkspaceId;
        token.Name = name.Trim();
        token.ExpiredDateTime = DateTimeUtcForPostgreSql.ToUtc(expiredDateTime);
        token.IsActive = isActive;
        token.IsPublic = isPublic;
        await dbContext.SaveChangesAsync(cancellationToken);
        return token;
    }

    /// <summary>
    ///     Syncs membership for one workspace: selected entities are assigned;
    ///     entities currently on this workspace but not selected are cleared.
    ///     Entities on other workspaces are left alone unless selected (then moved here).
    /// </summary>
    /// <returns>Number of entities assigned to the workspace after sync.</returns>
    public async Task<int> SetConnectionsWorkspaceAsync(
        Guid workspaceId,
        IReadOnlyList<Guid> connectionIds,
        CancellationToken cancellationToken = default)
    {
        var workspaceExists = await dbContext.GisWorkspaces.AnyAsync(w => w.Id == workspaceId, cancellationToken);
        if (!workspaceExists) throw new InvalidOperationException("Workspace was not found.");

        var selected = connectionIds.Distinct().ToHashSet();

        var connections = await dbContext.GisConnections
            .Where(c => selected.Contains(c.Id) || c.GisWorkspaceId == workspaceId)
            .ToListAsync(cancellationToken);

        foreach (var conn in connections)
        {
            if (selected.Contains(conn.Id))
                conn.GisWorkspaceId = workspaceId;
            else if (conn.GisWorkspaceId == workspaceId)
                conn.GisWorkspaceId = null;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return connections.Count(c => c.GisWorkspaceId == workspaceId);
    }

    private static string GenerateOpaqueToken()
    {
        var bytes = new byte[32];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }
}