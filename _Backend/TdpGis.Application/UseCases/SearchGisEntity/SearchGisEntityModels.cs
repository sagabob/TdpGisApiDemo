using System.Text.Json.Nodes;
using TdpGis.Application.Common;

namespace TdpGis.Application.UseCases.SearchGisEntity;

public sealed class SearchGisEntityQuery
{
    public required Guid WorkspaceId { get; init; }
    public required Guid EntityId { get; init; }
    public required string SearchedPhrase { get; init; }
    public required string? WorkspaceAccessToken { get; init; }
    public int MaxResults { get; init; } = SearchGisEntityLimits.DefaultMaxResults;
}

public sealed class SearchGisEntityResult
{
    public bool Succeeded { get; private init; }
    public GisQueryFailureKind? FailureKind { get; private init; }
    public string? ErrorMessage { get; private init; }
    public string? SearchedPhrase { get; private init; }
    public Guid EntityId { get; private init; }
    public List<JsonObject>? Collections { get; private init; }
    public bool WorkspaceTokenIsPublic { get; private init; }

    public static SearchGisEntityResult Success(
        string searchedPhrase,
        Guid entityId,
        List<JsonObject> collections,
        bool workspaceTokenIsPublic)
    {
        return new SearchGisEntityResult
        {
            Succeeded = true,
            SearchedPhrase = searchedPhrase,
            EntityId = entityId,
            Collections = collections,
            WorkspaceTokenIsPublic = workspaceTokenIsPublic
        };
    }

    public static SearchGisEntityResult Failure(GisQueryFailureKind kind)
    {
        return new SearchGisEntityResult
        {
            Succeeded = false,
            FailureKind = kind,
            ErrorMessage = GisQueryFailureMessages.For(kind)
        };
    }
}