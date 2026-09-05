using FastEndpoints;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using TdpGis.Api.GisQuery.Helpers;
using TdpGis.Api.GisQuery.Messages;
using TdpGis.Application.Abstractions;
using TdpGis.Application.AppModels;

namespace TdpGis.Api.GisQuery.Endpoints;

/// <summary>
///     GET /api/gis-workspace-entities/{workspaceId} — list GIS entities for a workspace (access token in headers).
/// </summary>
public sealed class GetGisWorkspaceEntitiesEndpoint(IGisConfigurationService configurationService)
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
        var accessError =
            await GisWorkspaceAccess.TryValidateAsync(configurationService, req.WorkspaceId, HttpContext.Request, ct);
        if (accessError is { } err)
        {
            await HttpContext.Response.SendAsync(
                new ApiMessageResponse { Message = err.Message },
                err.StatusCode,
                cancellation: ct);
            return;
        }

        var entities = configurationService.GetGisConnectionDtoByWorkspaceId(req.WorkspaceId);
        await Send.OkAsync(entities, ct);
    }
}