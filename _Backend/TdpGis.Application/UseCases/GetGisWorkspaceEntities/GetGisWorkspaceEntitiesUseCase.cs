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

        var access = await WorkspaceAccessGuard.ValidateAsync(
            configurationService,
            query.WorkspaceId,
            query.WorkspaceAccessToken,
            cancellationToken);

        if (!access.IsValid)
            return GetGisWorkspaceEntitiesResult.Failure(access.FailureKind!.Value);

        var entities = await configurationService.GetGisConnectionDtosByWorkspaceIdAsync(
            query.WorkspaceId,
            cancellationToken);

        return GetGisWorkspaceEntitiesResult.Success(entities, access.Token!.IsPublic);
    }
}