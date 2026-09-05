using FastEndpoints;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using TdpGis.Api.GisQuery.Helpers;
using TdpGis.Api.GisQuery.Messages;
using TdpGis.Application.Abstractions;

namespace TdpGis.Api.GisQuery.Endpoints;

public sealed class SearchGisEntityEndpoint(IGisConfigurationService configurationService, IGisDataService dataService)
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
        var accessError =
            await GisWorkspaceAccess.TryValidateAsync(configurationService, req.WorkspaceId, HttpContext.Request, ct);
        if (accessError is { } err)
        {
            await SendErrorAsync(err.Message, err.StatusCode, ct);
            return;
        }

        var selectedEntity =
            await configurationService.GetGisConnectionDtoByEntityId(req.WorkspaceId, req.EntityId);

        if (selectedEntity is null)
        {
            await SendErrorAsync(
                "Requested entity is not in the provided workspace.",
                StatusCodes.Status404NotFound,
                ct);
            return;
        }

        var result = await dataService.GetSearchedInstances(selectedEntity, req.SearchedPhrase, 10, ct);
        await Send.OkAsync(
            new SearchGisEntityResponse
            {
                SearchedPhrase = req.SearchedPhrase,
                EntityId = req.EntityId,
                Collections = result
            },
            ct);
    }

    private Task SendErrorAsync(string message, int statusCode, CancellationToken ct)
    {
        return HttpContext.Response.SendAsync(new ApiMessageResponse { Message = message }, statusCode,
            cancellation: ct);
    }
}