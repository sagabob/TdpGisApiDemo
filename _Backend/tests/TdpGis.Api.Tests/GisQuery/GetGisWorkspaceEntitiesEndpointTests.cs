using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using TdpGis.Api.Tests.Support;
using TdpGis.Application.Abstractions;
using TdpGis.Application.AppModels;
using TdpGis.Domain;
using Xunit;

namespace TdpGis.Api.Tests.GisQuery;

[Collection("ApiIntegration")]
public class GetGisWorkspaceEntitiesEndpointTests
{
    [Fact]
    public async Task Get_returns_401_when_entra_bearer_is_missing()
    {
        var workspaceId = Guid.NewGuid();
        var repository = new Mock<IGisConfigurationService>(MockBehavior.Strict);

        await using var app = CreateApp(repository);
        using var client = app.CreateClient();

        var response = await client.GetAsync($"/api/gis-workspace-entities/{workspaceId}",
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Get_returns_400_when_workspace_x_access_token_is_missing_but_entra_present()
    {
        var workspaceId = Guid.NewGuid();
        var repository = new Mock<IGisConfigurationService>(MockBehavior.Strict);

        await using var app = CreateApp(repository);
        using var client = app.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", IntegrationTestAuth.TestBearerToken);

        var response = await client.GetAsync($"/api/gis-workspace-entities/{workspaceId}",
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Get_returns_entities_when_entra_and_workspace_tokens_are_valid()
    {
        var workspaceId = Guid.NewGuid();
        const string token = "valid-token";

        var repository = new Mock<IGisConfigurationService>(MockBehavior.Strict);
        repository
            .Setup(r => r.GetValidWorkspaceAccessTokenAsync(workspaceId, token, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GisWorkspaceAccessToken
            {
                Id = Guid.NewGuid(),
                GisWorkspaceId = workspaceId,
                Name = "test-token",
                AccessToken = token,
                ExpiredDateTime = DateTime.UtcNow.AddHours(1),
                IsActive = true,
                IsPublic = false,
                GisWorkspace = new GisWorkspace { Id = workspaceId, Name = "ws" }
            });
        repository
            .Setup(r => r.GetGisConnectionDtoByWorkspaceId(workspaceId))
            .Returns(
            [
                new GisConnectionDto
                {
                    Id = Guid.NewGuid(),
                    Name = "Places",
                    Entity = "places",
                    GeometryType = GeometryType.MultiPolygon,
                    QueryField = "place_name",
                    PropertyMappings =
                    [
                        new PropertyMapping
                        {
                            Id = Guid.NewGuid(),
                            ColumnType = PropertyType.Normal,
                            PropertyName = "place_name",
                            PropertyLabel = "placeName"
                        }
                    ],
                    EntityLabel = "Places",
                    Description = "sample"
                }
            ]);

        await using var app = CreateApp(repository);
        using var client = app.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", IntegrationTestAuth.TestBearerToken);
        client.DefaultRequestHeaders.Add("X-Access-Token", token);

        var response = await client.GetAsync($"/api/gis-workspace-entities/{workspaceId}",
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<List<GisConnectionDto>>(TestContext.Current
            .CancellationToken);
        body.Should().NotBeNull();
        body.Should().HaveCount(1);
        body![0].Name.Should().Be("Places");

        repository.Verify(r => r.GetValidWorkspaceAccessTokenAsync(workspaceId, token, It.IsAny<CancellationToken>()),
            Times.Once);
        repository.Verify(r => r.GetGisConnectionDtoByWorkspaceId(workspaceId), Times.Once);
    }

    private static WebApplicationFactory<Program> CreateApp(Mock<IGisConfigurationService> repository)
    {
        return new TdpGisApiWebApplicationFactory(services =>
        {
            services.RemoveAll<IGisConfigurationService>();
            services.AddSingleton(repository.Object);
        });
    }
}