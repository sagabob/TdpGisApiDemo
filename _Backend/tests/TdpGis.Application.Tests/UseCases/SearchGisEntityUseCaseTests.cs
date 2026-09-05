using System.Text.Json.Nodes;
using FluentAssertions;
using Moq;
using TdpGis.Application.Abstractions;
using TdpGis.Application.Common;
using TdpGis.Application.UseCases.SearchGisEntity;
using TdpGis.Domain;
using Xunit;

namespace TdpGis.Application.Tests.UseCases;

public class SearchGisEntityUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_returns_missing_token_failure_when_access_token_missing()
    {
        var sut = new SearchGisEntityUseCase(
            Mock.Of<IGisConfigurationService>(),
            Mock.Of<IGisDataService>());

        var result = await sut.ExecuteAsync(
            new SearchGisEntityQuery
            {
                WorkspaceId = Guid.NewGuid(),
                EntityId = Guid.NewGuid(),
                SearchedPhrase = "park",
                WorkspaceAccessToken = null
            },
            TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.FailureKind.Should().Be(GisQueryFailureKind.MissingWorkspaceAccessToken);
        result.ErrorMessage.Should().Contain(GisQueryFailureMessages.AccessTokenHeaderName);
    }

    [Fact]
    public async Task ExecuteAsync_returns_invalid_token_failure_when_workspace_token_invalid()
    {
        var workspaceId = Guid.NewGuid();
        var configuration = new Mock<IGisConfigurationService>(MockBehavior.Strict);
        configuration
            .Setup(c => c.GetValidWorkspaceAccessTokenAsync(workspaceId, "bad", It.IsAny<CancellationToken>()))
            .ReturnsAsync((GisWorkspaceAccessToken?)null);

        var sut = new SearchGisEntityUseCase(configuration.Object, Mock.Of<IGisDataService>());

        var result = await sut.ExecuteAsync(
            new SearchGisEntityQuery
            {
                WorkspaceId = workspaceId,
                EntityId = Guid.NewGuid(),
                SearchedPhrase = "park",
                WorkspaceAccessToken = "bad"
            },
            TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.FailureKind.Should().Be(GisQueryFailureKind.InvalidWorkspaceAccessToken);
    }

    [Fact]
    public async Task ExecuteAsync_returns_entity_not_found_when_entity_missing()
    {
        var workspaceId = Guid.NewGuid();
        var entityId = Guid.NewGuid();
        const string token = "ok";

        var configuration = new Mock<IGisConfigurationService>(MockBehavior.Strict);
        configuration
            .Setup(c => c.GetValidWorkspaceAccessTokenAsync(workspaceId, token, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GisWorkspaceAccessToken
            {
                Id = Guid.NewGuid(),
                GisWorkspaceId = workspaceId,
                Name = "t",
                AccessToken = token,
                ExpiredDateTime = DateTime.UtcNow.AddHours(1),
                IsActive = true,
                IsPublic = false,
                GisWorkspace = new GisWorkspace { Id = workspaceId, Name = "ws" }
            });
        configuration
            .Setup(c => c.GetGisConnectionForQueryAsync(workspaceId, entityId))
            .ReturnsAsync((GisConnection?)null);

        var sut = new SearchGisEntityUseCase(configuration.Object, Mock.Of<IGisDataService>());

        var result = await sut.ExecuteAsync(
            new SearchGisEntityQuery
            {
                WorkspaceId = workspaceId,
                EntityId = entityId,
                SearchedPhrase = "park",
                WorkspaceAccessToken = token
            },
            TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.FailureKind.Should().Be(GisQueryFailureKind.EntityNotFound);
    }

    [Fact]
    public async Task ExecuteAsync_returns_collections_when_valid()
    {
        var workspaceId = Guid.NewGuid();
        var entityId = Guid.NewGuid();
        const string token = "ok";
        const string phrase = "garden";

        var entity = new GisConnection
        {
            Id = entityId,
            Name = "Places",
            Description = "",
            GeometryType = GeometryType.Point,
            QueryField = "name",
            PropertyMappings = [],
            Entity = "places",
            EntityLabel = "Places",
            DataSourceId = Guid.NewGuid(),
            DataSource = new DataSourceSetting
            {
                Id = Guid.NewGuid(),
                ConnectionString = "mongodb://localhost",
                DatabaseType = SourceType.Mongodb
            }
        };

        var configuration = new Mock<IGisConfigurationService>(MockBehavior.Strict);
        configuration
            .Setup(c => c.GetValidWorkspaceAccessTokenAsync(workspaceId, token, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GisWorkspaceAccessToken
            {
                Id = Guid.NewGuid(),
                GisWorkspaceId = workspaceId,
                Name = "t",
                AccessToken = token,
                ExpiredDateTime = DateTime.UtcNow.AddHours(1),
                IsActive = true,
                IsPublic = false,
                GisWorkspace = new GisWorkspace { Id = workspaceId, Name = "ws" }
            });
        configuration
            .Setup(c => c.GetGisConnectionForQueryAsync(workspaceId, entityId))
            .ReturnsAsync(entity);

        var data = new Mock<IGisDataService>(MockBehavior.Strict);
        data.Setup(d => d.GetSearchedInstances(entity, phrase, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync([new JsonObject { ["placeName"] = "Botanic Garden" }]);

        var sut = new SearchGisEntityUseCase(configuration.Object, data.Object);

        var result = await sut.ExecuteAsync(
            new SearchGisEntityQuery
            {
                WorkspaceId = workspaceId,
                EntityId = entityId,
                SearchedPhrase = phrase,
                WorkspaceAccessToken = token,
                MaxResults = 10
            },
            TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        result.SearchedPhrase.Should().Be(phrase);
        result.EntityId.Should().Be(entityId);
        result.Collections.Should().HaveCount(1);
        result.Collections![0]["placeName"]!.GetValue<string>().Should().Be("Botanic Garden");
    }
}