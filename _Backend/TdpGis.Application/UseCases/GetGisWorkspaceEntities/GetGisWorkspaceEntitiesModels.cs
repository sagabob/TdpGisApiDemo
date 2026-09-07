using TdpGis.Application.AppModels;
using TdpGis.Application.Common;

namespace TdpGis.Application.UseCases.GetGisWorkspaceEntities;

public sealed class GetGisWorkspaceEntitiesQuery
{
    public required Guid WorkspaceId { get; init; }
    public required string? WorkspaceAccessToken { get; init; }
}

public sealed class GetGisWorkspaceEntitiesResult
{
    public bool Succeeded { get; private init; }
    public GisQueryFailureKind? FailureKind { get; private init; }
    public string? ErrorMessage { get; private init; }
    public List<GisConnectionDto>? Entities { get; private init; }
    public bool WorkspaceTokenIsPublic { get; private init; }

    public static GetGisWorkspaceEntitiesResult Success(
        List<GisConnectionDto> entities,
        bool workspaceTokenIsPublic)
    {
        return new GetGisWorkspaceEntitiesResult
        {
            Succeeded = true,
            Entities = entities,
            WorkspaceTokenIsPublic = workspaceTokenIsPublic
        };
    }

    public static GetGisWorkspaceEntitiesResult Failure(GisQueryFailureKind kind)
    {
        return new GetGisWorkspaceEntitiesResult
        {
            Succeeded = false,
            FailureKind = kind,
            ErrorMessage = GisQueryFailureMessages.For(kind)
        };
    }
}