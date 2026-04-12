using FluentAssertions;
using TdpGis.Api.GisQuery.Messages;
using Xunit;

namespace TdpGis.Api.Tests.GisQuery.Messages;

public class GisQueryMessageContractsTests
{
    [Fact]
    public void GetGisWorkspaceEntitiesRequest_has_workspace_route_property()
    {
        var id = Guid.NewGuid();
        var req = new GetGisWorkspaceEntitiesRequest { WorkspaceId = id };
        req.WorkspaceId.Should().Be(id);
    }

    [Fact]
    public void SearchGisEntityRequest_primary_constructor_assigns_route_values()
    {
        var workspaceId = Guid.NewGuid();
        var entityId = Guid.NewGuid();
        const string searchedPhrase = "garden";

        var request = new SearchGisEntityRequest(workspaceId, entityId, searchedPhrase);

        request.WorkspaceId.Should().Be(workspaceId);
        request.EntityId.Should().Be(entityId);
        request.SearchedPhrase.Should().Be(searchedPhrase);
    }
}