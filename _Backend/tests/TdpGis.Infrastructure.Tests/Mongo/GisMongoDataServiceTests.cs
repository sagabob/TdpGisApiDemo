using System.Text.Json.Nodes;
using FluentAssertions;
using MongoDB.Bson;
using Moq;
using TdpGis.AdminApplication.Abstractions;
using TdpGis.Domain;
using TdpGis.Infrastructure.Mongo;
using Xunit;

namespace TdpGis.Infrastructure.Tests.Mongo;

public class GisMongoDataServiceTests
{
    [Fact]
    public async Task GetSearchedInstances_ShouldQueryMongoAndMapConfiguredFields()
    {
        var metadata = new Mock<IMongoMetadataProvider>(MockBehavior.Strict);
        var queryRepository = new Mock<IGisMongoQueryRepository>(MockBehavior.Strict);

        const string connectionString = "mongodb://localhost:27017/testdb";
        const string resolvedDb = "testdb";
        const string searchText = "lon";

        metadata.Setup(m => m.GetDatabaseName(connectionString)).Returns(resolvedDb);
        queryRepository
            .Setup(q => q.SearchAsync(
                connectionString,
                resolvedDb,
                "places",
                "name",
                searchText,
                10,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                new BsonDocument
                {
                    ["name"] = "London",
                    ["meta"] = new BsonDocument { ["country"] = "UK" }
                }
            ]);

        var sut = new GisMongoDataService(metadata.Object, queryRepository.Object);
        var connection = new GisConnection
        {
            Id = Guid.NewGuid(),
            Name = "Places",
            Description = "sample",
            GeometryType = GeometryType.MultiPoint,
            QueryField = "name",
            Entity = "places",
            EntityLabel = "Places",
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
                }
            ],
            DataSource = new DataSourceSetting
            {
                Id = Guid.NewGuid(),
                ConnectionString = connectionString,
                DatabaseType = SourceType.Mongodb
            }
        };

        var result = await sut.GetSearchedInstances(connection, searchText, 10, TestContext.Current.CancellationToken);

        result.Should().HaveCount(1);
        result[0]["Name"]!.GetValue<string>().Should().Be("London");
        result[0]["Meta"].Should().BeOfType<JsonObject>().Subject["country"]!.GetValue<string>().Should().Be("UK");

        metadata.VerifyAll();
        queryRepository.VerifyAll();
    }
}