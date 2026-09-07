using Microsoft.Extensions.Logging;
using TdpGis.Application.Abstractions;
using TdpGis.Application.Common;
using TdpGis.Domain;

namespace TdpGis.Application.UseCases.SearchGisEntity;

/// <summary>
///     Phrase query use case: validate workspace access, resolve entity, query GIS source by QueryField.
/// </summary>
public sealed class SearchGisEntityUseCase(
    IGisConfigurationService configurationService,
    IGisDataService dataService,
    ILogger<SearchGisEntityUseCase> logger) : ISearchGisEntityUseCase
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
        {
            logger.LogWarning(
                "GIS search access failed for workspace {WorkspaceId}: {FailureKind}",
                query.WorkspaceId,
                access.FailureKind);
            return SearchGisEntityResult.Failure(access.FailureKind!.Value);
        }

        var selectedEntity =
            await configurationService.GetGisConnectionForQueryAsync(query.WorkspaceId, query.EntityId);

        if (selectedEntity is null)
        {
            logger.LogWarning(
                "GIS search entity not found for workspace {WorkspaceId} entity {EntityId}",
                query.WorkspaceId,
                query.EntityId);
            return SearchGisEntityResult.Failure(GisQueryFailureKind.EntityNotFound);
        }

        if (selectedEntity.DataSource is null ||
            !selectedEntity.DataSource.DatabaseType.IsSupportedForGisQuery())
        {
            logger.LogWarning(
                "GIS search unsupported data source for workspace {WorkspaceId} entity {EntityId}",
                query.WorkspaceId,
                query.EntityId);
            return SearchGisEntityResult.Failure(GisQueryFailureKind.UnsupportedDataSourceType);
        }

        try
        {
            var collections = await dataService.GetSearchedInstances(
                selectedEntity,
                query.SearchedPhrase,
                SearchGisEntityLimits.Clamp(query.MaxResults),
                cancellationToken);

            logger.LogInformation(
                "GIS search completed for workspace {WorkspaceId} entity {EntityId}; collections={CollectionCount}",
                query.WorkspaceId,
                query.EntityId,
                collections.Count);

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
            logger.LogWarning(
                "GIS search unsupported data source type for workspace {WorkspaceId} entity {EntityId}",
                query.WorkspaceId,
                query.EntityId);
            return SearchGisEntityResult.Failure(GisQueryFailureKind.UnsupportedDataSourceType);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "GIS search data source query failed for workspace {WorkspaceId} entity {EntityId}",
                query.WorkspaceId,
                query.EntityId);
            return SearchGisEntityResult.Failure(GisQueryFailureKind.DataSourceQueryFailed);
        }
    }
}
