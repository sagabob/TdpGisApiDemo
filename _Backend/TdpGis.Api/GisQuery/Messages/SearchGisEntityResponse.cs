using System.Text.Json.Nodes;

namespace TdpGis.Api.GisQuery.Messages;

/// <summary>Successful GIS entity search payload.</summary>
public sealed class SearchGisEntityResponse
{
    public required string SearchedPhrase { get; init; }

    public Guid EntityId { get; init; }

    public required List<JsonObject> Collections { get; init; }
}