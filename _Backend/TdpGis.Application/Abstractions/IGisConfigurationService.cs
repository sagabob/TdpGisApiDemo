using TdpGis.Application.AppModels;
using TdpGis.Domain;

namespace TdpGis.Application.Abstractions;

public interface IGisConfigurationService
{
    Task<bool> HasEntityAsync(Guid workspaceId, Guid entityId);

    /// <summary>
    ///     Returns the access token row if it matches the workspace, secret, is active, and not expired.
    /// </summary>
    Task<GisWorkspaceAccessToken?> GetValidWorkspaceAccessTokenAsync(
        Guid workspaceId,
        string accessToken,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     GIS connections assigned to the workspace, as public DTOs (no connection strings).
    /// </summary>
    Task<List<GisConnectionDto>> GetGisConnectionDtosByWorkspaceIdAsync(
        Guid workspaceId,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Full GIS connection for a query operation (includes data source credentials and mappings).
    /// </summary>
    Task<GisConnection?> GetGisConnectionForQueryAsync(Guid workspaceId, Guid entityId);
}
