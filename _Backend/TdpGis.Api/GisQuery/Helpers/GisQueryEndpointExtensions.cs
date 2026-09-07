using FastEndpoints;
using TdpGis.Api.GisQuery.Messages;
using TdpGis.Application.Common;

namespace TdpGis.Api.GisQuery.Helpers;

/// <summary>Shared HTTP mapping for GIS query use-case failures.</summary>
public static class GisQueryEndpointExtensions
{
    public const string WorkspaceTokenPublicHeader = "X-TdpGis-Workspace-Token-Public";

    public static Task SendGisQueryFailureAsync(
        this HttpContext httpContext,
        string message,
        GisQueryFailureKind kind,
        CancellationToken cancellationToken)
    {
        return httpContext.Response.SendAsync(
            new ApiMessageResponse { Message = message },
            GisQueryHttp.StatusCode(kind),
            cancellation: cancellationToken);
    }

    public static void SetWorkspaceTokenPublicHeader(this HttpContext httpContext, bool isPublic)
    {
        httpContext.Response.Headers[WorkspaceTokenPublicHeader] = isPublic ? "true" : "false";
    }
}