using TdpGis.Application.Abstractions;
using TdpGis.Application.Common;
using TdpGis.Domain;

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

        var access = await WorkspaceAccessGuard.ValidateAsync(
            configurationService,
            query.WorkspaceId,
            query.WorkspaceAccessToken,
            cancellationToken);

        if (!access.IsValid)
            return SearchGisEntityResult.Failure(access.FailureKind!.Value);

        var selectedEntity =
            await configurationService.GetGisConnectionForQueryAsync(query.WorkspaceId, query.EntityId);

        if (selectedEntity is null)
            return SearchGisEntityResult.Failure(GisQueryFailureKind.EntityNotFound);

        if (selectedEntity.DataSource is null ||
            !selectedEntity.DataSource.DatabaseType.IsSupportedForGisQuery())
            return SearchGisEntityResult.Failure(GisQueryFailureKind.UnsupportedDataSourceType);

        try
        {
            var collections = await dataService.GetSearchedInstances(
                selectedEntity,
                query.SearchedPhrase,
                SearchGisEntityLimits.Clamp(query.MaxResults),
                cancellationToken);

            return SearchGisEntityResult.Success(
                query.SearchedPhrase,
                query.EntityId,
                collections,
                access.Token!.IsPublic);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (NotSupportedException)
        {
            return SearchGisEntityResult.Failure(GisQueryFailureKind.UnsupportedDataSourceType);
        }
        catch (Exception)
        {
            return SearchGisEntityResult.Failure(GisQueryFailureKind.DataSourceQueryFailed);
        }
    }
}