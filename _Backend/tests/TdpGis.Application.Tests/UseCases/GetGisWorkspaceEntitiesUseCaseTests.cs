using FluentAssertions;
using Moq;
using TdpGis.Application.Abstractions;
using TdpGis.Application.AppModels;
using TdpGis.Application.Common;
using TdpGis.Application.UseCases.GetGisWorkspaceEntities;
using TdpGis.Domain;
using Xunit;

namespace TdpGis.Application.Tests.UseCases;

public class GetGisWorkspaceEntitiesUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_returns_missing_token_when_header_absent()
    {
        var sut = new GetGisWorkspaceEntitiesUseCase(Mock.Of<IGisConfigurationService>());

        var result = await sut.ExecuteAsync(
            new GetGisWorkspaceEntitiesQuery
            {
                WorkspaceId = Guid.NewGuid(),
                WorkspaceAccessToken = " "
            },
            TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.FailureKind.Should().Be(GisQueryFailureKind.MissingWorkspaceAccessToken);
    }

    [Fact]
    public async Task ExecuteAsync_returns_entities_when_valid()
    {
        var workspaceId = Guid.NewGuid();
        const string token = "ok";
        var entities = new List<GisConnectionDto>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Places",
                Entity = "places",
                GeometryType = GeometryType.Point,
                QueryField = "name",
                PropertyMappings = [],
                EntityLabel = "Places",
                Description = ""
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
            .Setup(c => c.GetGisConnectionDtosByWorkspaceIdAsync(workspaceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entities);

        var sut = new GetGisWorkspaceEntitiesUseCase(configuration.Object);

        var result = await sut.ExecuteAsync(
            new GetGisWorkspaceEntitiesQuery
            {
                WorkspaceId = workspaceId,
                WorkspaceAccessToken = token
            },
            TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        result.Entities.Should().BeEquivalentTo(entities);
    }
}