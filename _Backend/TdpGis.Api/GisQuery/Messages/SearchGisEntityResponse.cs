using System.Text.Json.Nodes;

namespace TdpGis.Api.GisQuery.Messages;

/// <summary>Successful payload for a GIS entity phrase query (by configured QueryField).</summary>
public sealed class SearchGisEntityResponse
{
    public required string SearchedPhrase { get; init; }

    public Guid EntityId { get; init; }

    public required List<JsonObject> Collections { get; init; }

    /// <summary>Whether the workspace access token used for this call is marked public in admin.</summary>
    public bool WorkspaceTokenIsPublic { get; init; }
}