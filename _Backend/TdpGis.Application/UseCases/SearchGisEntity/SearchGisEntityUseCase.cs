using TdpGis.Application.Abstractions;
using TdpGis.Application.Common;

namespace TdpGis.Application.UseCases.SearchGisEntity;

/// <summary>
///     Phrase query use case: validate workspace access, resolve entity, query GIS source by QueryField.
/// </summary>
public sealed class SearchGisEntityUseCase(
    IGisConfigurationService configurationService,
    IGisDataService dataService) : ISearchGisEntityUseCase
{
    public async Task<SearchGisEntityResult> ExecuteAsync(
        SearchGisEntityQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var accessFailure = await WorkspaceAccessGuard.ValidateAsync(
            configurationService,
            query.WorkspaceId,
            query.WorkspaceAccessToken,
            cancellationToken);

        if (accessFailure is { } kind)
            return SearchGisEntityResult.Failure(kind);

        var selectedEntity =
            await configurationService.GetGisConnectionForQueryAsync(query.WorkspaceId, query.EntityId);

        if (selectedEntity is null)
            return SearchGisEntityResult.Failure(GisQueryFailureKind.EntityNotFound);

        var collections = await dataService.GetSearchedInstances(
            selectedEntity,
            query.SearchedPhrase,
            query.MaxResults,
            cancellationToken);

        return SearchGisEntityResult.Success(query.SearchedPhrase, query.EntityId, collections);
    }
}