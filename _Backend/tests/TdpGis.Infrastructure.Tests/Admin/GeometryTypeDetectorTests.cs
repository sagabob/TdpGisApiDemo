using FluentAssertions;
using TdpGis.AdminApplication.AppModels;
using TdpGis.Domain;
using Xunit;

namespace TdpGis.Infrastructure.Tests.Admin;

public class GeometryTypeDetectorTests
{
    [Fact]
    public void DetectFromSampleJson_ShouldReturnPolygon_ForWktPolygonInGeomField()
    {
        var json =
            """
            {
              "parkId": 2,
              "parkName": "Westminster Park",
              "parkTypeDescription": "Sports Park",
              "geom": "POLYGON ((172.648057573664 -43.4997082696735, 172.648028997381 -43.4997399196163, 172.648627899451 -43.500045096367))"
            }
            """;

        GeometryTypeDetector.DetectFromSampleJson(json).Should().Be(GeometryType.Polygon);
    }

    [Fact]
    public void DetectFromSampleJson_ShouldReturnMultiPolygon_OnlyForMultiPolygonWkt()
    {
        var json = """{ "geom": "MULTIPOLYGON (((0 0, 1 0, 1 1, 0 0)))" }""";
        GeometryTypeDetector.DetectFromSampleJson(json).Should().Be(GeometryType.MultiPolygon);
    }

    [Fact]
    public void DetectFromSampleJson_ShouldReturnPoint_ForWktPoint()
    {
        var json = """{ "geom": "POINT (172.64 -43.49)" }""";
        GeometryTypeDetector.DetectFromSampleJson(json).Should().Be(GeometryType.Point);
    }

    [Fact]
    public void DetectFromSampleJson_ShouldReturnPolygon_ForEwktWithSridPrefix()
    {
        var json = """{ "geom": "SRID=4326;POLYGON((0 0, 1 0, 1 1, 0 0))" }""";
        GeometryTypeDetector.DetectFromSampleJson(json).Should().Be(GeometryType.Polygon);
    }
}
