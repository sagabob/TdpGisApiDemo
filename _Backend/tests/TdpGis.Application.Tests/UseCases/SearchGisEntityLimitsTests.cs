using FluentAssertions;
using TdpGis.Application.UseCases.SearchGisEntity;
using Xunit;

namespace TdpGis.Application.Tests.UseCases;

public class SearchGisEntityLimitsTests
{
    [Theory]
    [InlineData(null, 10)]
    [InlineData(0, 10)]
    [InlineData(-1, 10)]
    [InlineData(5, 5)]
    [InlineData(100, 100)]
    [InlineData(101, 100)]
    public void Clamp_ShouldApplyDefaultsAndCap(int? requested, int expected)
    {
        SearchGisEntityLimits.Clamp(requested).Should().Be(expected);
    }
}