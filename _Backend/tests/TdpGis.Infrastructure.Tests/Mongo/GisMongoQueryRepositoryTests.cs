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
        using var runner = MongoDbRunner.Start();
        var client = new MongoClient(runner.ConnectionString);
        await WaitUntilReadyAsync(client, TestContext.Current.CancellationToken);
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
        using var runner = MongoDbRunner.Start();
        var client = new MongoClient(runner.ConnectionString);
        await WaitUntilReadyAsync(client, TestContext.Current.CancellationToken);
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

    private static async Task WaitUntilReadyAsync(IMongoClient client, CancellationToken ct)
    {
        // Mongo2Go can return before mongod is fully ready on some environments.
        for (var attempt = 0; attempt < 30; attempt++)
        {
            try
            {
                await client.GetDatabase("admin")
                    .RunCommandAsync<BsonDocument>(new BsonDocument("ping", 1), cancellationToken: ct);
                return;
            }
            catch (MongoConnectionException) when (!ct.IsCancellationRequested)
            {
                await Task.Delay(200, ct);
            }
            catch (TimeoutException) when (!ct.IsCancellationRequested)
            {
                await Task.Delay(200, ct);
            }
        }

        throw new TimeoutException("Mongo2Go server did not become ready in time.");
    }
}