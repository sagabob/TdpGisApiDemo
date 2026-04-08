using FluentAssertions;
using TdpGis.Infrastructure.Mongo;
using Xunit;

namespace TdpGis.Infrastructure.Tests.Mongo;

public class MongoClientCacheTests
{
    [Fact]
    public void GetOrCreate_ShouldReuseClient_ForEquivalentTrimmedConnectionStrings()
    {
        var cache = new MongoClientCache();
        const string cs = "mongodb://localhost:27017";

        var first = cache.GetOrCreate(cs);
        var second = cache.GetOrCreate($"  {cs}  ");

        second.Should().BeSameAs(first);
    }
}