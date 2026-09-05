using FluentAssertions;
using TdpGis.Domain;
using TdpGis.Infrastructure.Persistence;
using Xunit;

namespace TdpGis.Infrastructure.Tests.Persistence;

public class GisConfigurationServiceQueryTests
{
    [Fact]
    public async Task GetGisConnectionDtosByWorkspaceIdAsync_ShouldReturnSortedDtos_ForWorkspace()
    {
        await using var fixture = await SqliteDbContextFactory.CreateAsync();
        var workspace = new GisWorkspace { Id = Guid.NewGuid(), Name = "ws" };
        var source = new DataSourceSetting
        {
            Id = Guid.NewGuid(),
            Name = "Test source",
            ConnectionString = "mongodb://localhost:27017/db",
            DatabaseType = SourceType.Mongodb
        };
        fixture.DbContext.GisConnections.AddRange(
            new GisConnection
            {
                Id = Guid.NewGuid(),
                Name = "Zoo",
                Description = "d2",
                GeometryType = GeometryType.MultiPoint,
                QueryField = "q",
                PropertyMappings =
                [
                    new PropertyMapping
                    {
                        Id = Guid.NewGuid(), PropertyName = "p2", PropertyLabel = "P2", ColumnType = PropertyType.Normal
                    }
                ],
                Entity = "z",
                EntityLabel = "Z",
                GisWorkspace = workspace,
                DataSource = source
            },
            new GisConnection
            {
                Id = Guid.NewGuid(),
                Name = "Alpha",
                Description = "d1",
                GeometryType = GeometryType.MultiPolygon,
                QueryField = "q",
                PropertyMappings =
                [
                    new PropertyMapping
                    {
                        Id = Guid.NewGuid(), PropertyName = "p1", PropertyLabel = "P1", ColumnType = PropertyType.Object
                    }
                ],
                Entity = "a",
                EntityLabel = "A",
                GisWorkspace = workspace,
                DataSource = source
            });
        await fixture.DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var sut = new GisConfigurationService(fixture.DbContext);
        var result = await sut.GetGisConnectionDtosByWorkspaceIdAsync(
            workspace.Id,
            TestContext.Current.CancellationToken);

        result.Select(x => x.Name).Should().ContainInOrder("Alpha", "Zoo");
        result.Should().OnlyContain(x => x.PropertyMappings.Count == 1);
    }

    [Fact]
    public async Task HasEntityAsync_ShouldReturnTrueOnlyWhenWorkspaceAndEntityMatch()
    {
        await using var fixture = await SqliteDbContextFactory.CreateAsync();
        var workspace = new GisWorkspace { Id = Guid.NewGuid(), Name = "ws" };
        var source = new DataSourceSetting
        {
            Id = Guid.NewGuid(),
            Name = "Test source",
            ConnectionString = "mongodb://localhost:27017/db",
            DatabaseType = SourceType.Mongodb
        };
        var entityId = Guid.NewGuid();

        fixture.DbContext.GisConnections.Add(
            new GisConnection
            {
                Id = entityId,
                Name = "A",
                Description = "d",
                GeometryType = GeometryType.MultiPoint,
                QueryField = "q",
                PropertyMappings = [],
                Entity = "a",
                EntityLabel = "A",
                GisWorkspace = workspace,
                DataSource = source
            });
        await fixture.DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var sut = new GisConfigurationService(fixture.DbContext);
        (await sut.HasEntityAsync(workspace.Id, entityId)).Should().BeTrue();
        (await sut.HasEntityAsync(Guid.NewGuid(), entityId)).Should().BeFalse();
    }
}