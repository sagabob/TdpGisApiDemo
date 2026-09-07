using FastEndpoints;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using TdpGis.Api.GisQuery.Helpers;
using TdpGis.Api.GisQuery.Messages;
using TdpGis.Application.UseCases.SearchGisEntity;

namespace TdpGis.Api.GisQuery.Endpoints;

/// <summary>HTTP adapter for <see cref="ISearchGisEntityUseCase" />.</summary>
public sealed class SearchGisEntityEndpoint(ISearchGisEntityUseCase searchGisEntity)
    : Endpoint<SearchGisEntityRequest, SearchGisEntityResponse>
{
    public override void Configure()
    {
        Get("/api/gis-workspace/{workspaceId}/entity/{entityId}/search/{searchedPhrase}");
        AuthSchemes(JwtBearerDefaults.AuthenticationScheme);
        Summary(s =>
        {
            s.Summary =
                "Phrase query: returns mapped GIS rows for an entity where QueryField matches the search phrase.";
            s.Description =
                $"Path: `workspaceId`, `entityId`, `searchedPhrase`. Optional query `maxResults` (default {SearchGisEntityLimits.DefaultMaxResults}, max {SearchGisEntityLimits.AbsoluteMaxResults}). Send `Authorization: Bearer` (Entra) and `{GisWorkspaceAccess.AccessTokenHeader}` (workspace access token).";
            s.Params["maxResults"] = "Optional max rows to return (1–100).";
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
                MaxResults = SearchGisEntityLimits.Clamp(req.MaxResults)
            },
            ct);

        if (!result.Succeeded)
        {
            await HttpContext.SendGisQueryFailureAsync(result.ErrorMessage!, result.FailureKind!.Value, ct);
            return;
        }

        HttpContext.SetWorkspaceTokenPublicHeader(result.WorkspaceTokenIsPublic);

        await Send.OkAsync(
            new SearchGisEntityResponse
            {
                SearchedPhrase = result.SearchedPhrase!,
                EntityId = result.EntityId,
                Collections = result.Collections!,
                WorkspaceTokenIsPublic = result.WorkspaceTokenIsPublic
            },
            ct);
    }
}