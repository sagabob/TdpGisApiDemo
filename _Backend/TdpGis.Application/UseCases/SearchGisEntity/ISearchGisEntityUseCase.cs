namespace TdpGis.Application.UseCases.SearchGisEntity;

public interface ISearchGisEntityUseCase
{
    Task<SearchGisEntityResult> ExecuteAsync(SearchGisEntityQuery query, CancellationToken cancellationToken = default);
}
