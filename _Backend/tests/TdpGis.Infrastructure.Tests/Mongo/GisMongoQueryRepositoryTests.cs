using FluentAssertions;
using Mongo2Go;
using MongoDB.Bson;
using MongoDB.Driver;
using TdpGis.Infrastructure.Mongo;
using Xunit;

namespace TdpGis.Infrastructure.Tests.Mongo;

public class GisMongoQueryRepositoryTests
{
    [Fact]
    public async Task SearchAsync_ShouldFilterCaseInsensitive_AndRespectMaxResults()
    {
        using var runner = MongoDbRunner.Start(singleNodeReplSet: true);
        var client = new MongoClient(runner.ConnectionString);
        var dbName = $"tdp_{Guid.NewGuid():N}";
        var collection = client.GetDatabase(dbName).GetCollection<BsonDocument>("places");

        await collection.InsertManyAsync(
        [
            new BsonDocument { ["name"] = "London", ["kind"] = "city" },
            new BsonDocument { ["name"] = "Londonderry", ["kind"] = "city" },
            new BsonDocument { ["name"] = "Paris", ["kind"] = "city" }
        ], null, TestContext.Current.CancellationToken);

        var sut = new GisMongoQueryRepository(new MongoClientCache());
        var results = await sut.SearchAsync(
            runner.ConnectionString,
            dbName,
            "places",
            "name",
            "london",
            1,
            TestContext.Current.CancellationToken);

        results.Should().HaveCount(1);
        results[0]["name"].AsString.Should().StartWith("London");
    }

    [Fact]
    public async Task SearchAsync_ShouldReturnEmpty_WhenNoMatches()
    {
        using var runner = MongoDbRunner.Start(singleNodeReplSet: true);
        var client = new MongoClient(runner.ConnectionString);
        var dbName = $"tdp_{Guid.NewGuid():N}";
        var collection = client.GetDatabase(dbName).GetCollection<BsonDocument>("places");

        await collection.InsertOneAsync(
            new BsonDocument { ["name"] = "Tokyo" },
            cancellationToken: TestContext.Current.CancellationToken);

        var sut = new GisMongoQueryRepository(new MongoClientCache());
        var results = await sut.SearchAsync(
            runner.ConnectionString,
            dbName,
            "places",
            "name",
            "london",
            10,
            TestContext.Current.CancellationToken);

        results.Should().BeEmpty();
    }
}