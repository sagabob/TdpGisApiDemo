using FastEndpoints;
using TdpGis.Application.Abstractions;

namespace TdpGis.Api.GisQuery;

/// <summary>
///     GET /api/gis-entities — list GIS entities for a workspace (headers: X-Workspace-Id, X-Access-Token or Bearer).
/// </summary>
public sealed class GetGisWorkspaceEntitiesEndpoint(IGisConfigurationRepository repository) : EndpointWithoutRequest
{
    public const string WorkspaceIdHeader = "X-Workspace-Id";
    public const string AccessTokenHeader = "X-Access-Token";

    public override void Configure()
    {
        Get("/api/gis-workspace-entities");
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "Returns GIS entity definitions for the workspace when the access token is valid.";
            s.Description =
                $"Requires `{WorkspaceIdHeader}` and `{AccessTokenHeader}` (or Authorization: Bearer).";
        });
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var request = HttpContext.Request;

        if (!request.Headers.TryGetValue(WorkspaceIdHeader, out var workspaceRaw) ||
            !Guid.TryParse(workspaceRaw.ToString(), out var workspaceId))
        {
            await HttpContext.Response.SendAsync(
                new { message = $"Header '{WorkspaceIdHeader}' is required and must be a valid GUID." },
                400,
                cancellation: ct);
            return;
        }

        var accessToken = ResolveAccessToken(request);
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

        var validToken = await repository.GetValidWorkspaceAccessTokenAsync(workspaceId, accessToken, ct);
        if (validToken is null)
        {
            await HttpContext.Response.SendAsync(
                new { message = "Invalid workspace, access token, or token is inactive or expired." },
                401,
                cancellation: ct);
            return;
        }

        var entities = repository.GetGisConnectionDtoByWorkspaceId(workspaceId);
        await HttpContext.Response.SendAsync(entities, cancellation: ct);
    }

    private static string? ResolveAccessToken(HttpRequest request)
    {
        if (request.Headers.TryGetValue(AccessTokenHeader, out var direct) && !string.IsNullOrWhiteSpace(direct))
            return direct.ToString().Trim();

        var auth = request.Headers.Authorization.ToString();
        if (auth.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)) return auth["Bearer ".Length..].Trim();

        return null;
    }
}