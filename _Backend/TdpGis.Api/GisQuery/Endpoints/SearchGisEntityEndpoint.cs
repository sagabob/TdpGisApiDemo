using FastEndpoints;
using TdpGis.Api.GisQuery.Messages;
using TdpGis.Application.Abstractions;

namespace TdpGis.Api.GisQuery.Endpoints;

public class SearchGisEntityEndpoint(IGisConfigurationService repository, IGisDataService dataService)
    : Endpoint<SearchGisEntityRequest>
{
    public override void Configure()
    {
        Get("/api/gis-workspace/{workspaceId}/entity/{entityId}/search/{searchedPhrase}");
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "Returns GIS collection for a given entity satisfying searchable phrase.";
            s.Description =
                $"Path: `workspaceId`. Provide `{GisWorkspaceAccess.AccessTokenHeader}` or `Authorization: Bearer` (access token value).";
        });
    }

    public override async Task HandleAsync(SearchGisEntityRequest req, CancellationToken ct)
    {
        var accessError =
            await GisWorkspaceAccess.TryValidateAsync(repository, req.WorkspaceId, HttpContext.Request, ct);
        if (accessError is { } err)
            await HttpContext.Response.SendAsync(new { message = err.Message }, err.StatusCode, cancellation: ct);

        var selectedEntity = await repository.GetGisConnectionDtoByEntityId(req.WorkspaceId, req.EntityId);


        if (selectedEntity != null)
        {
            var result = await dataService.GetSearchedInstances(selectedEntity, req.SearchedPhrase, 10, ct);

            // TODO: implement GIS entity search against Mongo using workspace/entity configuration.
            await HttpContext.Response.SendAsync(
                new { req.SearchedPhrase, entityId = req.EntityId, collections = result },
                cancellation: ct);
        }
        else
        {
            await HttpContext.Response.SendAsync(
                new { message = "Requested entity is not in the provided workspace." },
                401,
                cancellation: ct);
        }
    }
}