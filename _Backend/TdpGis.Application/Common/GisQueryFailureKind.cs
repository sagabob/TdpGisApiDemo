namespace TdpGis.Application.Common;

/// <summary>Application-level failures for GIS query use cases (HTTP mapping lives at the API edge).</summary>
public enum GisQueryFailureKind
{
    InvalidWorkspaceId,
    MissingWorkspaceAccessToken,
    InvalidWorkspaceAccessToken,
    EntityNotFound,
    UnsupportedDataSourceType,
    DataSourceQueryFailed
}

public static class GisQueryFailureMessages
{
    public const string AccessTokenHeaderName = "X-Access-Token";

    public static string For(GisQueryFailureKind kind)
    {
        return kind switch
        {
            GisQueryFailureKind.InvalidWorkspaceId =>
                "A valid workspace id is required.",
            GisQueryFailureKind.MissingWorkspaceAccessToken =>
                $"Provide header '{AccessTokenHeaderName}' with the workspace access token (Entra token must be sent as Authorization: Bearer separately).",
            GisQueryFailureKind.InvalidWorkspaceAccessToken =>
                "Invalid workspace, access token, or token is inactive or expired.",
            GisQueryFailureKind.EntityNotFound =>
                "Requested entity is not in the provided workspace.",
            GisQueryFailureKind.UnsupportedDataSourceType =>
                "The entity's data source type is not supported for GIS queries.",
            GisQueryFailureKind.DataSourceQueryFailed =>
                "The GIS data source query failed.",
            _ => "Request failed."
        };
    }
}