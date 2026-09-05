using TdpGis.Application.Common;

namespace TdpGis.Api.GisQuery.Helpers;

/// <summary>Maps application GIS query failures to HTTP status codes.</summary>
public static class GisQueryHttp
{
    public static int StatusCode(GisQueryFailureKind kind) => kind switch
    {
        GisQueryFailureKind.InvalidWorkspaceId => StatusCodes.Status400BadRequest,
        GisQueryFailureKind.MissingWorkspaceAccessToken => StatusCodes.Status400BadRequest,
        GisQueryFailureKind.InvalidWorkspaceAccessToken => StatusCodes.Status401Unauthorized,
        GisQueryFailureKind.EntityNotFound => StatusCodes.Status404NotFound,
        _ => StatusCodes.Status400BadRequest
    };
}
