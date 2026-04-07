using System.ComponentModel;
using FastEndpoints;
using FluentAssertions;
using TdpGis.Api.GisQuery.Helpers;
using TdpGis.Api.GisQuery.Messages;
using Xunit;

namespace TdpGis.Api.Tests.GisQuery.Messages;

public class GisQueryMessageContractsTests
{
    [Fact]
    public void GetGisWorkspaceEntitiesRequest_has_expected_access_token_header_attributes()
    {
        var prop = typeof(GetGisWorkspaceEntitiesRequest).GetProperty(
            nameof(GetGisWorkspaceEntitiesRequest.AccessToken));

        prop.Should().NotBeNull();
        var fromHeader = prop!.GetCustomAttributes(typeof(FromHeaderAttribute), false)
            .Cast<FromHeaderAttribute>()
            .Single();
        var defaultValue = prop.GetCustomAttributes(typeof(DefaultValueAttribute), false)
            .Cast<DefaultValueAttribute>()
            .Single();

        fromHeader.HeaderName.Should().Be(GisWorkspaceAccess.AccessTokenHeader);
        fromHeader.IsRequired.Should().BeTrue();
        defaultValue.Value.Should().Be(string.Empty);
    }

    [Fact]
    public void SearchGisEntityRequest_has_expected_access_token_header_attributes()
    {
        var prop = typeof(SearchGisEntityRequest).GetProperty(nameof(SearchGisEntityRequest.AccessToken));

        prop.Should().NotBeNull();
        var fromHeader = prop!.GetCustomAttributes(typeof(FromHeaderAttribute), false)
            .Cast<FromHeaderAttribute>()
            .Single();
        var defaultValue = prop.GetCustomAttributes(typeof(DefaultValueAttribute), false)
            .Cast<DefaultValueAttribute>()
            .Single();

        fromHeader.HeaderName.Should().Be(GisWorkspaceAccess.AccessTokenHeader);
        fromHeader.IsRequired.Should().BeTrue();
        defaultValue.Value.Should().Be(string.Empty);
    }

    [Fact]
    public void SearchGisEntityRequest_primary_constructor_assigns_route_values()
    {
        var workspaceId = Guid.NewGuid();
        var entityId = Guid.NewGuid();
        const string searchedPhrase = "garden";

        var request = new SearchGisEntityRequest(workspaceId, entityId, searchedPhrase)
        {
            AccessToken = Guid.NewGuid().ToString()
        };

        request.WorkspaceId.Should().Be(workspaceId);
        request.EntityId.Should().Be(entityId);
        request.SearchedPhrase.Should().Be(searchedPhrase);
        request.AccessToken.Should().NotBeNullOrEmpty();
    }
}