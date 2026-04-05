using FastEndpoints;
using TdpGis.Api.GisQuery.Messages;
using TdpGis.Application.Abstractions;

namespace TdpGis.Api.GisQuery.Endpoints;

public class SearchGisEntityEndpoint(IGisConfigurationService repository) : Endpoint<SearchGisEntityRequest>
{
    public const string AccessTokenHeader = "X-Access-Token";

    public override void Configure()
    {
        Get("/api/gis-workspace/{workspaceId}/entity/{entityId}/search/{searchedPhrase}");
        AllowAnonymous();
    }

    public override async Task HandleAsync(SearchGisEntityRequest req, CancellationToken ct)
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

        var hasEntity = await repository.HasEntityAsync(req.WorkspaceId, req.EntityId);

        if (!hasEntity)
        {
            await HttpContext.Response.SendAsync(
                new { message = "Requested entity is not in the provided workspace." },
                401,
                cancellation: ct);
            return;
        }

        // TODO: implement GIS entity search against Mongo using workspace/entity configuration.
        await HttpContext.Response.SendAsync(
            new { req.SearchedPhrase, features = Array.Empty<object>() },
            cancellation: ct);
    }

    private static string? ResolveAccessToken(HttpRequest request)
    {
        if (request.Headers.TryGetValue(AccessTokenHeader, out var direct) && !string.IsNullOrWhiteSpace(direct))
            return direct.ToString().Trim();

        var auth = request.Headers.Authorization.ToString();
        return auth.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase) ? auth["Bearer ".Length..].Trim() : null;
    }
}