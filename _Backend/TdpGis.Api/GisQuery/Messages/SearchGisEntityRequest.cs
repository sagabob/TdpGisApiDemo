namespace TdpGis.Api.GisQuery.Messages;

/// <summary>
///     Route-only. Workspace token is validated in the SearchGisEntity use case after
///     <see cref="TdpGis.Api.GisQuery.Helpers.GisWorkspaceAccess.ResolveAccessToken" />.
/// </summary>
public record SearchGisEntityRequest(Guid WorkspaceId, Guid EntityId, string SearchedPhrase);