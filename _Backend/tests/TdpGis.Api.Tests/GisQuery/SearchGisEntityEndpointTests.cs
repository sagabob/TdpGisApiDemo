using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using TdpGis.Api.GisQuery.Messages;
using TdpGis.Api.Tests.Support;
using TdpGis.Application.Abstractions;
using TdpGis.Domain;
using Xunit;

namespace TdpGis.Api.Tests.GisQuery;

[Collection("ApiIntegration")]
public class SearchGisEntityEndpointTests
{
    [Fact]
    public async Task Get_returns_401_when_entra_bearer_is_missing()
    {
        var workspaceId = Guid.NewGuid();
        var entityId = Guid.NewGuid();

        var repository = new Mock<IGisConfigurationService>(MockBehavior.Strict);
        var dataService = new Mock<IGisDataService>(MockBehavior.Strict);

        await using var app = CreateApp(repository, dataService);
        using var client = app.CreateClient();

        var response = await client.GetAsync(
            $"/api/gis-workspace/{workspaceId}/entity/{entityId}/search/garden",
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Get_returns_404_when_entity_is_not_in_workspace()
    {
        var workspaceId = Guid.NewGuid();
        var entityId = Guid.NewGuid();
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
            .Setup(r => r.GetGisConnectionForQueryAsync(workspaceId, entityId))
            .ReturnsAsync((GisConnection?)null);

        var dataService = new Mock<IGisDataService>(MockBehavior.Strict);

        await using var app = CreateApp(repository, dataService);
        using var client = app.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", IntegrationTestAuth.TestBearerToken);
        client.DefaultRequestHeaders.Add("X-Access-Token", token);

        var response = await client.GetAsync(
            $"/api/gis-workspace/{workspaceId}/entity/{entityId}/search/garden",
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var body = await response.Content.ReadFromJsonAsync<ApiMessageResponse>(
            TestContext.Current.CancellationToken);
        body.Should().NotBeNull();
        body!.Message.Should().Be("Requested entity is not in the provided workspace.");
    }

    [Fact]
    public async Task Get_returns_collections_when_access_and_entity_are_valid()
    {
        var workspaceId = Guid.NewGuid();
        var entityId = Guid.NewGuid();
        const string token = "valid-token";
        const string searchedPhrase = "garden";

        var selectedEntity = new GisConnection
        {
            Id = entityId,
            Name = "Places",
            Description = "Sample",
            GeometryType = GeometryType.MultiPolygon,
            QueryField = "name",
            PropertyMappings =
            [
                new PropertyMapping
                {
                    Id = Guid.NewGuid(),
                    ColumnType = PropertyType.Normal,
                    PropertyName = "name",
                    PropertyLabel = "placeName"
                }
            ],
            Entity = "places",
            EntityLabel = "Places",
            DataSourceId = Guid.NewGuid(),
            DataSource = new DataSourceSetting
            {
                Id = Guid.NewGuid(),
                Name = "Test source",
                ConnectionString = "mongodb://localhost:27017/db",
                DatabaseType = SourceType.Mongodb
            }
        };

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
            .Setup(r => r.GetGisConnectionForQueryAsync(workspaceId, entityId))
            .ReturnsAsync(selectedEntity);

        var dataService = new Mock<IGisDataService>(MockBehavior.Strict);
        dataService
            .Setup(d => d.GetSearchedInstances(selectedEntity, searchedPhrase, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                new JsonObject
                {
                    ["placeName"] = "Botanic Garden"
                }
            ]);

        await using var app = CreateApp(repository, dataService);
        using var client = app.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", IntegrationTestAuth.TestBearerToken);
        client.DefaultRequestHeaders.Add("X-Access-Token", token);

        var response = await client.GetAsync(
            $"/api/gis-workspace/{workspaceId}/entity/{entityId}/search/{searchedPhrase}",
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<SearchGisEntityResponse>(
            TestContext.Current.CancellationToken);
        body.Should().NotBeNull();
        body!.SearchedPhrase.Should().Be(searchedPhrase);
        body.EntityId.Should().Be(entityId);
        body.Collections.Should().HaveCount(1);
        body.Collections[0]["placeName"]!.GetValue<string>().Should().Be("Botanic Garden");

        repository.Verify(r => r.GetValidWorkspaceAccessTokenAsync(workspaceId, token, It.IsAny<CancellationToken>()),
            Times.Once);
        repository.Verify(r => r.GetGisConnectionForQueryAsync(workspaceId, entityId), Times.Once);
        dataService.Verify(
            d => d.GetSearchedInstances(selectedEntity, searchedPhrase, 10, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    private static WebApplicationFactory<Program> CreateApp(
        Mock<IGisConfigurationService> repository,
        Mock<IGisDataService> dataService)
    {
        return new TdpGisApiWebApplicationFactory(services =>
        {
            services.RemoveAll<IGisConfigurationService>();
            services.RemoveAll<IGisDataService>();
            services.AddSingleton(repository.Object);
            services.AddSingleton(dataService.Object);
        });
    }
}