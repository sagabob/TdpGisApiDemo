using System.Text.Json.Nodes;
using FluentAssertions;
using Moq;
using TdpGis.Domain;
using TdpGis.Infrastructure.Sql;
using Xunit;

namespace TdpGis.Infrastructure.Tests.Sql;

public class GisSqlDataServiceTests
{
    [Theory]
    [InlineData(SourceType.Postgres)]
    [InlineData(SourceType.SqlServer)]
    public async Task GetSearchedInstances_ShouldQuerySqlAndMapConfiguredFields(SourceType databaseType)
    {
        var queryRepository = new Mock<IGisSqlQueryRepository>(MockBehavior.Strict);
        const string connectionString = "Host=localhost;Database=gis";
        const string searchText = "park";

        queryRepository
            .Setup(q => q.SearchAsync(
                databaseType,
                connectionString,
                "ccc.parks",
                "name",
                searchText,
                It.Is<IReadOnlyList<string>>(cols => cols.SequenceEqual(new[] { "name", "meta", "geom" })),
                10,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                new Dictionary<string, object?>
                {
                    ["name"] = "Central Park",
                    ["meta"] = """{"city":"NYC"}""",
                    ["geom"] = "POINT(1 2)"
                }
            ]);

        var sut = new GisSqlDataService(queryRepository.Object);
        var connection = new GisConnection
        {
            Id = Guid.NewGuid(),
            Name = "Parks",
            Description = "sample",
            GeometryType = GeometryType.Point,
            QueryField = "name",
            Entity = "ccc.parks",
            EntityLabel = "Parks",
            PropertyMappings =
            [
                new PropertyMapping
                {
                    Id = Guid.NewGuid(),
                    PropertyName = "name",
                    PropertyLabel = "Name",
                    ColumnType = PropertyType.Normal
                },
                new PropertyMapping
                {
                    Id = Guid.NewGuid(),
                    PropertyName = "meta",
                    PropertyLabel = "Meta",
                    ColumnType = PropertyType.Object
                },
                new PropertyMapping
                {
                    Id = Guid.NewGuid(),
                    PropertyName = "geom",
                    PropertyLabel = "Geometry",
                    ColumnType = PropertyType.Normal
                }
            ],
            DataSource = new DataSourceSetting
            {
                Id = Guid.NewGuid(),
                Name = "Test source",
                ConnectionString = connectionString,
                DatabaseType = databaseType
            }
        };

        var result = await sut.GetSearchedInstances(connection, searchText, 10, TestContext.Current.CancellationToken);

        result.Should().HaveCount(1);
        result[0]["Name"]!.GetValue<string>().Should().Be("Central Park");
        result[0]["Geometry"]!.GetValue<string>().Should().Be("POINT(1 2)");
        result[0]["Meta"].Should().BeOfType<JsonObject>().Subject["city"]!.GetValue<string>().Should().Be("NYC");

        queryRepository.VerifyAll();
    }

    [Fact]
    public async Task GetSearchedInstances_ShouldRejectMongo()
    {
        var sut = new GisSqlDataService(Mock.Of<IGisSqlQueryRepository>());
        var connection = new GisConnection
        {
            Id = Guid.NewGuid(),
            Name = "Places",
            Description = "sample",
            GeometryType = GeometryType.Point,
            QueryField = "name",
            Entity = "places",
            EntityLabel = "Places",
            PropertyMappings = [],
            DataSource = new DataSourceSetting
            {
                Id = Guid.NewGuid(),
                Name = "Test source",
                ConnectionString = "mongodb://localhost",
                DatabaseType = SourceType.Mongodb
            }
        };

        var act = async () =>
            await sut.GetSearchedInstances(connection, "x", 10, TestContext.Current.CancellationToken);
        await act.Should().ThrowAsync<NotSupportedException>();
    }
}