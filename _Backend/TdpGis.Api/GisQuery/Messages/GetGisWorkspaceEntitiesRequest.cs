namespace TdpGis.Api.GisQuery.Messages;

/// <summary>
///     Route-only binding. Workspace token is read from <c>X-Access-Token</c> via
///     <see cref="TdpGis.Api.GisQuery.Helpers.GisWorkspaceAccess.ResolveAccessToken" /> and validated
///     in the GetGisWorkspaceEntities use case (missing token → 400).
/// </summary>
public sealed class GetGisWorkspaceEntitiesRequest
{
    /// <summary>Workspace identifier (route).</summary>
    public Guid WorkspaceId { get; set; }
}