using FluentAssertions;
using TdpGis.Infrastructure.Helpers;
using Xunit;

namespace TdpGis.Infrastructure.Tests.Helpers;

public class DateTimeUtcForPostgreSqlTests
{
    [Fact]
    public void ToUtc_ShouldReturnUtcKind_ForUnspecifiedInput()
    {
        var input = new DateTime(2026, 1, 2, 3, 4, 5, DateTimeKind.Unspecified);

        var result = DateTimeUtcForPostgreSql.ToUtc(input);

        result.Kind.Should().Be(DateTimeKind.Utc);
    }

    [Fact]
    public void ToUtc_ShouldKeepSameMoment_ForUtcInput()
    {
        var input = new DateTime(2026, 1, 2, 3, 4, 5, DateTimeKind.Utc);

        var result = DateTimeUtcForPostgreSql.ToUtc(input);

        result.Should().Be(input);
        result.Kind.Should().Be(DateTimeKind.Utc);
    }

    [Fact]
    public void FromStore_ShouldForceUtcKind_ForUnspecifiedValue()
    {
        var input = new DateTime(2026, 1, 2, 3, 4, 5, DateTimeKind.Unspecified);

        var result = DateTimeUtcForPostgreSql.FromStore(input);

        result.Kind.Should().Be(DateTimeKind.Utc);
        result.Ticks.Should().Be(input.Ticks);
    }
}