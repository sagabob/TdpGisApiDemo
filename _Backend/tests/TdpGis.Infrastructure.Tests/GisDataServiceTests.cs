using System.Text.Json.Nodes;
using FluentAssertions;
using Moq;
using TdpGis.AdminApplication.Abstractions;
using TdpGis.Domain;
using TdpGis.Infrastructure;
using TdpGis.Infrastructure.Mongo;
using TdpGis.Infrastructure.Sql;
using Xunit;

namespace TdpGis.Infrastructure.Tests;

public class GisDataServiceTests
{
    [Fact]
    public async Task GetSearchedInstances_ShouldRouteMongoToMongoService()
    {
        var metadata = new Mock<IMongoMetadataProvider>(MockBehavior.Strict);
        var mongoQuery = new Mock<IGisMongoQueryRepository>(MockBehavior.Strict);
        var sqlQuery = new Mock<IGisSqlQueryRepository>(MockBehavior.Strict);

        metadata.Setup(m => m.GetDatabaseName(It.IsAny<string>())).Returns("db");
        mongoQuery
            .Setup(q => q.SearchAsync(
                It.IsAny<string>(),
                "db",
                "places",
                "name",
                "lon",
                5,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var sut = new GisDataService(
            new GisMongoDataService(metadata.Object, mongoQuery.Object),
            new GisSqlDataService(sqlQuery.Object));

        var connection = CreateConnection(SourceType.Mongodb, "mongodb://localhost/db", "places");
        var result = await sut.GetSearchedInstances(connection, "lon", 5, TestContext.Current.CancellationToken);

        result.Should().BeEmpty();
        mongoQuery.VerifyAll();
        sqlQuery.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(SourceType.Postgres)]
    [InlineData(SourceType.SqlServer)]
    public async Task GetSearchedInstances_ShouldRouteRelationalToSqlService(SourceType databaseType)
    {
        var metadata = new Mock<IMongoMetadataProvider>(MockBehavior.Strict);
        var mongoQuery = new Mock<IGisMongoQueryRepository>(MockBehavior.Strict);
        var sqlQuery = new Mock<IGisSqlQueryRepository>(MockBehavior.Strict);

        sqlQuery
            .Setup(q => q.SearchAsync(
                databaseType,
                It.IsAny<string>(),
                "ccc.parks",
                "name",
                "park",
                It.IsAny<IReadOnlyList<string>>(),
                5,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                new Dictionary<string, object?> { ["name"] = "Park" }
            ]);

        var sut = new GisDataService(
            new GisMongoDataService(metadata.Object, mongoQuery.Object),
            new GisSqlDataService(sqlQuery.Object));

        var connection = CreateConnection(databaseType, "Host=localhost", "ccc.parks");
        connection.PropertyMappings =
        [
            new PropertyMapping
            {
                Id = Guid.NewGuid(),
                PropertyName = "name",
                PropertyLabel = "Name",
                ColumnType = PropertyType.Normal
            }
        ];

        var result = await sut.GetSearchedInstances(connection, "park", 5, TestContext.Current.CancellationToken);

        result.Should().HaveCount(1);
        result[0]["Name"]!.GetValue<string>().Should().Be("Park");
        sqlQuery.VerifyAll();
        mongoQuery.VerifyNoOtherCalls();
        metadata.VerifyNoOtherCalls();
    }

    private static GisConnection CreateConnection(SourceType databaseType, string connectionString, string entity) =>
        new()
        {
            Id = Guid.NewGuid(),
            Name = "Entity",
            Description = "sample",
            GeometryType = GeometryType.Point,
            QueryField = "name",
            Entity = entity,
            EntityLabel = "Entity",
            PropertyMappings = [],
            DataSource = new DataSourceSetting
            {
                Id = Guid.NewGuid(),
                ConnectionString = connectionString,
                DatabaseType = databaseType
            }
        };
}
