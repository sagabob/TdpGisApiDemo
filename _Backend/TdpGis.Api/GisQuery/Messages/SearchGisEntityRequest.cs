using System.ComponentModel;
using FastEndpoints;
using TdpGis.Api.GisQuery.Helpers;

namespace TdpGis.Api.GisQuery.Messages;

public record SearchGisEntityRequest(Guid WorkspaceId, Guid EntityId, string SearchedPhrase)
{
    /// <summary>
    ///     Shown in Swagger; send the workspace access token here, or use <c>Authorization: Bearer</c> instead.
    /// </summary>
    [FromHeader(GisWorkspaceAccess.AccessTokenHeader)]
    [DefaultValue("")]
    public required string AccessToken { get; init; }
}