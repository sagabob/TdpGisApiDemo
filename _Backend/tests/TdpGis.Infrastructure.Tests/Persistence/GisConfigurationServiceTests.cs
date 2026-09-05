using FluentAssertions;
using TdpGis.Domain;
using TdpGis.Infrastructure.Persistence;
using Xunit;

namespace TdpGis.Infrastructure.Tests.Persistence;

public class GisConfigurationServiceTests
{
    [Fact]
    public async Task GetValidWorkspaceAccessTokenAsync_ShouldReturnNull_WhenTokenIsWhitespace()
    {
        await using var fixture = await SqliteDbContextFactory.CreateAsync();
        var sut = new GisConfigurationService(fixture.DbContext);

        var result = await sut.GetValidWorkspaceAccessTokenAsync(
            Guid.NewGuid(),
            "   ",
            TestContext.Current.CancellationToken);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetValidWorkspaceAccessTokenAsync_ShouldTrimInputAndReturnOnlyActiveNonExpiredToken()
    {
        var workspaceId = Guid.NewGuid();
        const string token = "valid-token";
        const string expiredToken = "expired-token";
        const string inactiveToken = "inactive-token";

        await using var fixture = await SqliteDbContextFactory.CreateAsync();
        var db = fixture.DbContext;
        var workspace = new GisWorkspace { Id = workspaceId, Name = "ws" };
        db.GisWorkspaceAccessTokens.AddRange(
            new GisWorkspaceAccessToken
            {
                Id = Guid.NewGuid(),
                GisWorkspaceId = workspaceId,
                Name = "valid",
                AccessToken = token,
                ExpiredDateTime = DateTime.UtcNow.AddMinutes(10),
                IsActive = true,
                IsPublic = false,
                GisWorkspace = workspace
            },
            new GisWorkspaceAccessToken
            {
                Id = Guid.NewGuid(),
                GisWorkspaceId = workspaceId,
                Name = "expired",
                AccessToken = expiredToken,
                ExpiredDateTime = DateTime.UtcNow.AddMinutes(-10),
                IsActive = true,
                IsPublic = false,
                GisWorkspace = workspace
            },
            new GisWorkspaceAccessToken
            {
                Id = Guid.NewGuid(),
                GisWorkspaceId = workspaceId,
                Name = "inactive",
                AccessToken = inactiveToken,
                ExpiredDateTime = DateTime.UtcNow.AddMinutes(10),
                IsActive = false,
                IsPublic = false,
                GisWorkspace = workspace
            });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var sut = new GisConfigurationService(db);
        var result = await sut.GetValidWorkspaceAccessTokenAsync(
            workspaceId,
            $"  {token}  ",
            TestContext.Current.CancellationToken);

        result.Should().NotBeNull();
        result!.Name.Should().Be("valid");
    }

    [Fact]
    public async Task GetValidWorkspaceAccessTokenAsync_ShouldReturnNull_WhenTokenIsExpired()
    {
        var workspaceId = Guid.NewGuid();
        const string token = "expired-token";

        await using var fixture = await SqliteDbContextFactory.CreateAsync();
        fixture.DbContext.GisWorkspaceAccessTokens.Add(
            new GisWorkspaceAccessToken
            {
                Id = Guid.NewGuid(),
                GisWorkspaceId = workspaceId,
                Name = "expired",
                AccessToken = token,
                ExpiredDateTime = DateTime.UtcNow.AddMinutes(-1),
                IsActive = true,
                IsPublic = false,
                GisWorkspace = new GisWorkspace { Id = workspaceId, Name = "ws" }
            });
        await fixture.DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var sut = new GisConfigurationService(fixture.DbContext);
        var result = await sut.GetValidWorkspaceAccessTokenAsync(
            workspaceId,
            token,
            TestContext.Current.CancellationToken);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetValidWorkspaceAccessTokenAsync_ShouldReturnNull_WhenTokenIsInactive()
    {
        var workspaceId = Guid.NewGuid();
        const string token = "inactive-token";

        await using var fixture = await SqliteDbContextFactory.CreateAsync();
        fixture.DbContext.GisWorkspaceAccessTokens.Add(
            new GisWorkspaceAccessToken
            {
                Id = Guid.NewGuid(),
                GisWorkspaceId = workspaceId,
                Name = "inactive",
                AccessToken = token,
                ExpiredDateTime = DateTime.UtcNow.AddMinutes(10),
                IsActive = false,
                IsPublic = false,
                GisWorkspace = new GisWorkspace { Id = workspaceId, Name = "ws" }
            });
        await fixture.DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var sut = new GisConfigurationService(fixture.DbContext);
        var result = await sut.GetValidWorkspaceAccessTokenAsync(
            workspaceId,
            token,
            TestContext.Current.CancellationToken);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetGisConnectionForQueryAsync_ShouldReturnConnectionWithDataSourceAndMappings_WhenFound()
    {
        await using var fixture = await SqliteDbContextFactory.CreateAsync();
        var workspace = new GisWorkspace { Id = Guid.NewGuid(), Name = "ws" };
        var source = new DataSourceSetting
        {
            Id = Guid.NewGuid(),
            ConnectionString = "mongodb://localhost:27017/db",
            DatabaseType = SourceType.Mongodb
        };
        var entityId = Guid.NewGuid();

        fixture.DbContext.GisConnections.Add(
            new GisConnection
            {
                Id = entityId,
                Name = "EntityA",
                Description = "d",
                GeometryType = GeometryType.MultiPolygon,
                QueryField = "name",
                PropertyMappings =
                [
                    new PropertyMapping
                    {
                        Id = Guid.NewGuid(),
                        PropertyName = "name",
                        PropertyLabel = "Name",
                        ColumnType = PropertyType.Normal
                    }
                ],
                Entity = "entity_a",
                EntityLabel = "Entity A",
                GisWorkspace = workspace,
                DataSource = source
            });
        await fixture.DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var sut = new GisConfigurationService(fixture.DbContext);
        var result = await sut.GetGisConnectionForQueryAsync(workspace.Id, entityId);

        result.Should().NotBeNull();
        result!.DataSource.Should().NotBeNull();
        result.PropertyMappings.Should().ContainSingle();
        result.Entity.Should().Be("entity_a");
    }

    [Fact]
    public async Task GetGisConnectionForQueryAsync_ShouldReturnNull_WhenWorkspaceOrEntityDoesNotMatch()
    {
        await using var fixture = await SqliteDbContextFactory.CreateAsync();
        var workspace = new GisWorkspace { Id = Guid.NewGuid(), Name = "ws" };
        var source = new DataSourceSetting
        {
            Id = Guid.NewGuid(),
            ConnectionString = "mongodb://localhost:27017/db",
            DatabaseType = SourceType.Mongodb
        };
        var entityId = Guid.NewGuid();

        fixture.DbContext.GisConnections.Add(
            new GisConnection
            {
                Id = entityId,
                Name = "EntityA",
                Description = "d",
                GeometryType = GeometryType.MultiPolygon,
                QueryField = "name",
                PropertyMappings = [],
                Entity = "entity_a",
                EntityLabel = "Entity A",
                GisWorkspace = workspace,
                DataSource = source
            });
        await fixture.DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var sut = new GisConfigurationService(fixture.DbContext);
        var wrongWorkspaceResult = await sut.GetGisConnectionForQueryAsync(Guid.NewGuid(), entityId);
        var wrongEntityResult = await sut.GetGisConnectionForQueryAsync(workspace.Id, Guid.NewGuid());

        wrongWorkspaceResult.Should().BeNull();
        wrongEntityResult.Should().BeNull();
    }
}