using FastEndpoints;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using TdpGis.Api.GisQuery.Helpers;
using TdpGis.Api.GisQuery.Messages;
using TdpGis.Application.AppModels;
using TdpGis.Application.UseCases.GetGisWorkspaceEntities;

namespace TdpGis.Api.GisQuery.Endpoints;

/// <summary>HTTP adapter for <see cref="IGetGisWorkspaceEntitiesUseCase" />.</summary>
public sealed class GetGisWorkspaceEntitiesEndpoint(IGetGisWorkspaceEntitiesUseCase getGisWorkspaceEntities)
    : Endpoint<GetGisWorkspaceEntitiesRequest, List<GisConnectionDto>>
{
    public override void Configure()
    {
        Get("/api/gis-workspace-entities/{workspaceId}");
        AuthSchemes(JwtBearerDefaults.AuthenticationScheme);
        Summary(s =>
        {
            s.Summary = "Returns GIS entity definitions for the workspace when tokens are valid.";
            s.Description =
                $"Path: `workspaceId`. Send `Authorization: Bearer` (Microsoft Entra access token) and `{GisWorkspaceAccess.AccessTokenHeader}` (workspace access token).";
        });
    }

    public override async Task HandleAsync(GetGisWorkspaceEntitiesRequest req, CancellationToken ct)
    {
        var result = await getGisWorkspaceEntities.ExecuteAsync(
            new GetGisWorkspaceEntitiesQuery
            {
                WorkspaceId = req.WorkspaceId,
                WorkspaceAccessToken = GisWorkspaceAccess.ResolveAccessToken(HttpContext.Request)
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

        await Send.OkAsync(result.Entities!, ct);
    }
}