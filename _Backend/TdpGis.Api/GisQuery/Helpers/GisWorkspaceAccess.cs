using TdpGis.Application.Abstractions;

namespace TdpGis.Api.GisQuery.Helpers;

/// <summary>
///     Resolves the GIS workspace access token from headers. Entra access tokens use
///     <c>Authorization: Bearer</c> and are validated separately by JWT bearer middleware — never mixed here.
/// </summary>
public static class GisWorkspaceAccess
{
    public const string AccessTokenHeader = "X-Access-Token";

    /// <summary>
    ///     Returns null when the workspace id and workspace access token are valid; otherwise the client error to send.
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
                $"Provide header '{AccessTokenHeader}' with the workspace access token (Entra token must be sent as Authorization: Bearer separately).",
                400);

        var validToken = await repository.GetValidWorkspaceAccessTokenAsync(workspaceId, accessToken, ct);
        if (validToken is null) return ("Invalid workspace, access token, or token is inactive or expired.", 401);

        return null;
    }

    /// <summary>
    ///     Returns the workspace-scoped access token from <see cref="AccessTokenHeader" /> only.
    ///     Does not read <c>Authorization: Bearer</c> — that value is the Microsoft Entra access token.
    /// </summary>
    public static string? ResolveAccessToken(HttpRequest request)
    {
        if (request.Headers.TryGetValue(AccessTokenHeader, out var direct) && !string.IsNullOrWhiteSpace(direct))
            return direct.ToString().Trim();

        return null;
    }
}