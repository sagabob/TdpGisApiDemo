namespace TdpGis.Application.UseCases.GetGisWorkspaceEntities;

public interface IGetGisWorkspaceEntitiesUseCase
{
    Task<GetGisWorkspaceEntitiesResult> ExecuteAsync(
        GetGisWorkspaceEntitiesQuery query,
        CancellationToken cancellationToken = default);
}