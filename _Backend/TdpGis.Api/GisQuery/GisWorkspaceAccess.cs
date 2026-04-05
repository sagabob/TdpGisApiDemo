using TdpGis.Application.Abstractions;

namespace TdpGis.Api.GisQuery;

/// <summary>
///     Shared access-token resolution and workspace token validation for GIS query endpoints.
/// </summary>
public static class GisWorkspaceAccess
{
    public const string AccessTokenHeader = "X-Access-Token";

    /// <summary>
    ///     Returns null when the workspace id and access token are valid; otherwise the client error to send.
    /// </summary>
    public static async Task<(string Message, int StatusCode)?> TryValidateAsync(
        IGisConfigurationService repository,
        Guid workspaceId,
        HttpRequest request,
        CancellationToken ct)
    {
        if (workspaceId == Guid.Empty)
            return ("A valid workspace id is required.", 400);

        var accessToken = ResolveAccessToken(request);
        if (string.IsNullOrWhiteSpace(accessToken))
            return (
                $"Provide header '{AccessTokenHeader}', or Authorization: Bearer (access token value).",
                400);

        var validToken = await repository.GetValidWorkspaceAccessTokenAsync(workspaceId, accessToken, ct);
        if (validToken is null) return ("Invalid workspace, access token, or token is inactive or expired.", 401);

        return null;
    }

    public static string? ResolveAccessToken(HttpRequest request)
    {
        if (request.Headers.TryGetValue(AccessTokenHeader, out var direct) && !string.IsNullOrWhiteSpace(direct))
            return direct.ToString().Trim();

        var auth = request.Headers.Authorization.ToString();
        return auth.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase) ? auth["Bearer ".Length..].Trim() : null;
    }
}