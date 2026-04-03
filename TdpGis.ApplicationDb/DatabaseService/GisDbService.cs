using System.Security.Cryptography;
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
            .Include(x => x.GisWorkspace)
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

    public async Task<GisWorkspace?> UpdateWorkspaceAsync(Guid id, string name, CancellationToken cancellationToken = default)
    {
        var workspace = await dbContext.GisWorkspaces.FirstOrDefaultAsync(w => w.Id == id, cancellationToken);
        if (workspace is null)
        {
            return null;
        }

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
        if (workspace is null)
        {
            throw new InvalidOperationException("Workspace was not found.");
        }

        var token = new GisWorkspaceAccessToken
        {
            Id = Guid.NewGuid(),
            GisWorkspaceId = workspaceId,
            Name = name.Trim(),
            AccessToken = GenerateOpaqueToken(),
            ExpiredDateTime = expiredDateTime,
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
        if (token is null)
        {
            return null;
        }

        var workspaceExists = await dbContext.GisWorkspaces.AnyAsync(w => w.Id == gisWorkspaceId, cancellationToken);
        if (!workspaceExists)
        {
            throw new InvalidOperationException("Workspace was not found.");
        }

        token.GisWorkspaceId = gisWorkspaceId;
        token.Name = name.Trim();
        token.ExpiredDateTime = expiredDateTime;
        token.IsActive = isActive;
        token.IsPublic = isPublic;
        await dbContext.SaveChangesAsync(cancellationToken);
        return token;
    }

    public async Task<int> SetConnectionsWorkspaceAsync(
        Guid workspaceId,
        IReadOnlyList<Guid> connectionIds,
        CancellationToken cancellationToken = default)
    {
        if (connectionIds.Count == 0)
        {
            return 0;
        }

        var workspaceExists = await dbContext.GisWorkspaces.AnyAsync(w => w.Id == workspaceId, cancellationToken);
        if (!workspaceExists)
        {
            throw new InvalidOperationException("Workspace was not found.");
        }

        var connections = await dbContext.GisConnections
            .Where(c => connectionIds.Contains(c.Id))
            .ToListAsync(cancellationToken);

        foreach (var conn in connections)
        {
            conn.GisWorkspaceId = workspaceId;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return connections.Count;
    }

    private static string GenerateOpaqueToken()
    {
        var bytes = new byte[32];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }
}