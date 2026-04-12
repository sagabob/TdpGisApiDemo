namespace TdpGis.Api.GisQuery.Messages;

/// <summary>
///     Route-only. Workspace token is validated via
///     <see cref="TdpGis.Api.GisQuery.Helpers.GisWorkspaceAccess.TryValidateAsync" />.
/// </summary>
public record SearchGisEntityRequest(Guid WorkspaceId, Guid EntityId, string SearchedPhrase);