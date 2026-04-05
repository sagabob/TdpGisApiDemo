using Microsoft.EntityFrameworkCore;
using TdpGis.Application.Abstractions;
using TdpGis.Application.AppModels;
using TdpGis.Domain;

namespace TdpGis.Infrastructure.Persistence;

public class GisConfigurationService(GisAppDbContext dbContext) : IGisConfigurationService
{
    public List<GisConnectionDto> GetGisConnectionDtoByWorkspaceId(Guid workspaceId)
    {
        return dbContext.GisConnections
            .AsNoTracking()
            .Include(x => x.PropertyMappings)
            .Where(x => x.GisWorkspaceId == workspaceId)
            .OrderBy(x => x.Name)
            .Select(x => new GisConnectionDto
            {
                Id = x.Id,
                Name = x.Name,
                Entity = x.Entity,
                GeometryType = x.GeometryType,
                QueryField = x.QueryField,
                PropertyMappings = x.PropertyMappings.ToList(),
                EntityLabel = x.EntityLabel,
                Description = x.Description
            })
            .ToList();
    }

    public async Task<GisConnection?> GetGisConnectionDtoByEntityId(Guid entityId, Guid workspaceId)
    {
        return await dbContext.GisConnections
            .AsNoTracking()
            .Include(x => x.PropertyMappings)
            .Where(x => x.GisWorkspaceId == workspaceId && x.Id == entityId).FirstOrDefaultAsync();
        ;
    }


    public async Task<bool> HasEntityAsync(Guid workspaceId, Guid entityId)
    {
        return await dbContext.GisConnections
            .AsNoTracking()
            .AnyAsync(x => x.GisWorkspaceId == workspaceId && x.Id == entityId);
    }

    public async Task<GisWorkspaceAccessToken?> GetValidWorkspaceAccessTokenAsync(
        Guid workspaceId,
        string accessToken,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(accessToken)) return null;

        var trimmed = accessToken.Trim();
        var now = DateTime.UtcNow;
        return await dbContext.GisWorkspaceAccessTokens
            .AsNoTracking()
            .FirstOrDefaultAsync(
                t => t.GisWorkspaceId == workspaceId
                     && t.AccessToken == trimmed
                     && t.IsActive
                     && t.ExpiredDateTime > now,
                cancellationToken);
    }
}