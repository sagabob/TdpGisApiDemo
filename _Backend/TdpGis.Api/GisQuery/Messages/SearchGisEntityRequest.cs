using System.ComponentModel;
using FastEndpoints;

namespace TdpGis.Api.GisQuery.Messages;

public record SearchGisEntityRequest(Guid WorkspaceId, Guid EntityId, string SearchedPhrase)
{
    /// <summary>
    ///     Shown in Swagger; send the workspace access token here, or use <c>Authorization: Bearer</c> instead.
    /// </summary>
    [FromHeader(GisWorkspaceAccess.AccessTokenHeader, false)]
    [DefaultValue("")]
    public string? AccessToken { get; init; }
}