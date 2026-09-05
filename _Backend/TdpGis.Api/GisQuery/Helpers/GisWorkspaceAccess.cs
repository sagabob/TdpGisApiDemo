using TdpGis.Application.Common;

namespace TdpGis.Api.GisQuery.Helpers;

/// <summary>
///     Resolves the GIS workspace access token from headers. Entra access tokens use
///     <c>Authorization: Bearer</c> and are validated separately by JWT bearer middleware — never mixed here.
/// </summary>
public static class GisWorkspaceAccess
{
    public const string AccessTokenHeader = GisQueryFailureMessages.AccessTokenHeaderName;

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