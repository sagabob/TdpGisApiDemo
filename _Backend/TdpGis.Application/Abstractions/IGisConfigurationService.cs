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
    List<GisConnectionDto> GetGisConnectionDtoByWorkspaceId(Guid workspaceId);

    Task<GisConnection?> GetGisConnectionDtoByEntityId(Guid entityId, Guid workspaceId);
}