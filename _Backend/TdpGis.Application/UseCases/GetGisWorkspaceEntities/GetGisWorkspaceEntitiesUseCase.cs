using TdpGis.Application.Abstractions;
using TdpGis.Application.Common;

namespace TdpGis.Application.UseCases.GetGisWorkspaceEntities;

/// <summary>List GIS entity definitions for a workspace after validating the workspace access token.</summary>
public sealed class GetGisWorkspaceEntitiesUseCase(IGisConfigurationService configurationService)
    : IGetGisWorkspaceEntitiesUseCase
{
    public async Task<GetGisWorkspaceEntitiesResult> ExecuteAsync(
        GetGisWorkspaceEntitiesQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var accessFailure = await WorkspaceAccessGuard.ValidateAsync(
            configurationService,
            query.WorkspaceId,
            query.WorkspaceAccessToken,
            cancellationToken);

        if (accessFailure is { } kind)
            return GetGisWorkspaceEntitiesResult.Failure(kind);

        var entities = await configurationService.GetGisConnectionDtosByWorkspaceIdAsync(
            query.WorkspaceId,
            cancellationToken);

        return GetGisWorkspaceEntitiesResult.Success(entities);
    }
}