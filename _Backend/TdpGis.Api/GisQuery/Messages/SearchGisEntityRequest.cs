namespace TdpGis.Api.GisQuery.Messages;

public record SearchGisEntityRequest(Guid WorkspaceId, Guid EntityId, string SearchedPhrase);