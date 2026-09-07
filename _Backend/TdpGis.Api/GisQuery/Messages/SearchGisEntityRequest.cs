namespace TdpGis.Api.GisQuery.Messages;

/// <summary>
///     Route + optional query. Workspace token is validated in the SearchGisEntity use case after
///     <see cref="TdpGis.Api.GisQuery.Helpers.GisWorkspaceAccess.ResolveAccessToken" />.
/// </summary>
public sealed class SearchGisEntityRequest
{
    public Guid WorkspaceId { get; init; }

    public Guid EntityId { get; init; }

    public string SearchedPhrase { get; init; } = string.Empty;

    /// <summary>Optional; defaults to 10, capped at 100.</summary>
    public int? MaxResults { get; init; }
}