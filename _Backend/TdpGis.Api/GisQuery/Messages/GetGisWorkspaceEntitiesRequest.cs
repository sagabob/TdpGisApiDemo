namespace TdpGis.Api.GisQuery.Messages;

public sealed class GetGisWorkspaceEntitiesRequest
{
    /// <summary>Workspace identifier (route).</summary>
    public Guid WorkspaceId { get; set; }
}