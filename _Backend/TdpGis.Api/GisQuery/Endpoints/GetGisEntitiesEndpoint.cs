using FastEndpoints;
using TdpGis.Api.GisQuery.Messages;
using TdpGis.Application.Abstractions;

namespace TdpGis.Api.GisQuery.Endpoints;

/// <summary>
///     GET /api/gis-workspace-entities/{workspaceId} — list GIS entities for a workspace (access token in headers).
/// </summary>
public sealed class GetGisWorkspaceEntitiesEndpoint(IGisConfigurationService repository)
    : Endpoint<GetGisWorkspaceEntitiesRequest>
{
    public const string AccessTokenHeader = "X-Access-Token";

    public override void Configure()
    {
        Get("/api/gis-workspace-entities/{workspaceId}");
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "Returns GIS entity definitions for the workspace when the access token is valid.";
            s.Description =
                $"Path: `workspaceId`. Provide `{AccessTokenHeader}` or `Authorization: Bearer` (access token value).";
        });
    }

    public override async Task HandleAsync(GetGisWorkspaceEntitiesRequest req, CancellationToken ct)
    {
        if (req.WorkspaceId == Guid.Empty)
        {
            await HttpContext.Response.SendAsync(
                new { message = "A valid workspace id is required." },
                400,
                cancellation: ct);
            return;
        }

        var accessToken = ResolveAccessToken(HttpContext.Request);
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            await HttpContext.Response.SendAsync(
                new
                {
                    message = $"Provide header '{AccessTokenHeader}', or Authorization: Bearer (access token value)."
                },
                400,
                cancellation: ct);
            return;
        }

        var validToken = await repository.GetValidWorkspaceAccessTokenAsync(req.WorkspaceId, accessToken, ct);
        if (validToken is null)
        {
            await HttpContext.Response.SendAsync(
                new { message = "Invalid workspace, access token, or token is inactive or expired." },
                401,
                cancellation: ct);
            return;
        }

        var entities = repository.GetGisConnectionDtoByWorkspaceId(req.WorkspaceId);
        await HttpContext.Response.SendAsync(entities, cancellation: ct);
    }

    private static string? ResolveAccessToken(HttpRequest request)
    {
        if (request.Headers.TryGetValue(AccessTokenHeader, out var direct) && !string.IsNullOrWhiteSpace(direct))
            return direct.ToString().Trim();

        var auth = request.Headers.Authorization.ToString();
        return auth.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase) ? auth["Bearer ".Length..].Trim() : null;
    }
}