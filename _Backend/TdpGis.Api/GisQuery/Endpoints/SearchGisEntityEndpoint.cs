using FastEndpoints;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using TdpGis.Api.GisQuery.Helpers;
using TdpGis.Api.GisQuery.Messages;
using TdpGis.Application.UseCases.SearchGisEntity;

namespace TdpGis.Api.GisQuery.Endpoints;

/// <summary>HTTP adapter for <see cref="ISearchGisEntityUseCase"/>.</summary>
public sealed class SearchGisEntityEndpoint(ISearchGisEntityUseCase searchGisEntity)
    : Endpoint<SearchGisEntityRequest, SearchGisEntityResponse>
{
    public override void Configure()
    {
        Get("/api/gis-workspace/{workspaceId}/entity/{entityId}/search/{searchedPhrase}");
        AuthSchemes(JwtBearerDefaults.AuthenticationScheme);
        Summary(s =>
        {
            s.Summary = "Phrase query: returns mapped GIS rows for an entity where QueryField matches the search phrase.";
            s.Description =
                $"Path: `workspaceId`, `entityId`, `searchedPhrase`. One of the GIS query operations (others such as spatial search may be added later). Send `Authorization: Bearer` (Entra) and `{GisWorkspaceAccess.AccessTokenHeader}` (workspace access token).";
        });
    }

    public override async Task HandleAsync(SearchGisEntityRequest req, CancellationToken ct)
    {
        var result = await searchGisEntity.ExecuteAsync(
            new SearchGisEntityQuery
            {
                WorkspaceId = req.WorkspaceId,
                EntityId = req.EntityId,
                SearchedPhrase = req.SearchedPhrase,
                WorkspaceAccessToken = GisWorkspaceAccess.ResolveAccessToken(HttpContext.Request),
                MaxResults = 10
            },
            ct);

        if (!result.Succeeded)
        {
            await HttpContext.Response.SendAsync(
                new ApiMessageResponse { Message = result.ErrorMessage! },
                GisQueryHttp.StatusCode(result.FailureKind!.Value),
                cancellation: ct);
            return;
        }

        await Send.OkAsync(
            new SearchGisEntityResponse
            {
                SearchedPhrase = result.SearchedPhrase!,
                EntityId = result.EntityId,
                Collections = result.Collections!
            },
            ct);
    }
}
