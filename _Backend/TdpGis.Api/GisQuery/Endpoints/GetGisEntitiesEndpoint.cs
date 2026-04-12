using FastEndpoints;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using TdpGis.Api.GisQuery.Helpers;
using TdpGis.Api.GisQuery.Messages;
using TdpGis.Application.Abstractions;

namespace TdpGis.Api.GisQuery.Endpoints;

/// <summary>
///     GET /api/gis-workspace-entities/{workspaceId} — list GIS entities for a workspace (access token in headers).
/// </summary>
public sealed class GetGisWorkspaceEntitiesEndpoint(IGisConfigurationService repository)
    : Endpoint<GetGisWorkspaceEntitiesRequest>
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
            await GisWorkspaceAccess.TryValidateAsync(repository, req.WorkspaceId, HttpContext.Request, ct);
        if (accessError is { } err)
        {
            await HttpContext.Response.SendAsync(new { message = err.Message }, err.StatusCode, cancellation: ct);
            return;
        }

        var entities = repository.GetGisConnectionDtoByWorkspaceId(req.WorkspaceId);
        await HttpContext.Response.SendAsync(entities, cancellation: ct);
    }
}