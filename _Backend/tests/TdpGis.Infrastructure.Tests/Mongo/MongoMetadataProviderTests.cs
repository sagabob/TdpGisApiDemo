using FluentAssertions;
using TdpGis.Infrastructure.Mongo;
using Xunit;

namespace TdpGis.Infrastructure.Tests.Mongo;

public class MongoMetadataProviderTests
{
    [Fact]
    public void GetDatabaseName_ShouldReturnDatabaseName_FromConnectionString()
    {
        var sut = new MongoMetadataProvider(new MongoClientCache());

        var result = sut.GetDatabaseName("  mongodb://localhost:27017/mydb  ");

        result.Should().Be("mydb");
    }

    [Fact]
    public async Task ProbeConnectionAsync_ShouldReturnInvalid_WhenConnectionStringIsMissing()
    {
        var sut = new MongoMetadataProvider(new MongoClientCache());

        var result = await sut.ProbeConnectionAsync("   ", null, TestContext.Current.CancellationToken);

        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("connection string is required");
        result.DatabaseName.Should().BeNull();
        result.Collections.Should().BeNull();
    }

    [Fact]
    public async Task ProbeConnectionAsync_ShouldReturnInvalid_WhenDatabaseIsMissing()
    {
        var sut = new MongoMetadataProvider(new MongoClientCache());

        var result =
            await sut.ProbeConnectionAsync("mongodb://localhost:27017", null, TestContext.Current.CancellationToken);

        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Database name is required");
        result.DatabaseName.Should().BeNull();
        result.Collections.Should().BeNull();
    }

    [Fact]
    public async Task ProbeConnectionAsync_ShouldReturnInvalid_WhenMongoConnectionFails()
    {
        var sut = new MongoMetadataProvider(new MongoClientCache());
        const string cs = "mongodb://127.0.0.1:1/testdb?serverSelectionTimeoutMS=100&connectTimeoutMS=100";

        var result = await sut.ProbeConnectionAsync(cs, null, TestContext.Current.CancellationToken);

        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().StartWith("MongoDB connection failed:");
        result.DatabaseName.Should().BeNull();
        result.Collections.Should().BeNull();
    }
}