using System.ComponentModel;
using FastEndpoints;

namespace TdpGis.Api.GisQuery.Messages;

public sealed class GetGisWorkspaceEntitiesRequest
{
    /// <summary>Workspace identifier (route).</summary>
    public Guid WorkspaceId { get; set; }

    /// <summary>
    ///     Shown in Swagger; send the workspace access token here, or use <c>Authorization: Bearer</c> instead.
    /// </summary>
    [FromHeader(GisWorkspaceAccess.AccessTokenHeader, false)]
    [DefaultValue("")]
    public string? AccessToken { get; set; }
}