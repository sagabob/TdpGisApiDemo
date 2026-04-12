namespace TdpGis.Api.GisQuery.Messages;

/// <summary>
///     Route-only binding. Workspace token is read from <c>X-Access-Token</c> in
///     <see cref="TdpGis.Api.GisQuery.Helpers.GisWorkspaceAccess.TryValidateAsync" /> (not as a required
///     <c>[FromHeader]</c> property), so a missing workspace token returns 400 from that validation.
/// </summary>
public sealed class GetGisWorkspaceEntitiesRequest
{
    /// <summary>Workspace identifier (route).</summary>
    public Guid WorkspaceId { get; set; }
}