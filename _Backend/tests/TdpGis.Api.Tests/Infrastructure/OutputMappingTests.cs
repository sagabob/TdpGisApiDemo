using FluentAssertions;
using MongoDB.Bson;
using TdpGis.Domain;
using TdpGis.Infrastructure.Helpers;
using Xunit;

namespace TdpGis.Api.Tests.Infrastructure;

public class OutputMappingTests
{
    [Fact]
    public void ConvertFromBson_maps_normal_fields_as_string_values()
    {
        var doc = new BsonDocument
        {
            { "place_name", "Botanic Garden" },
            { "place_id", 42 }
        };

        var maps = new List<PropertyMapping>
        {
            new() { ColumnType = PropertyType.Normal, PropertyName = "place_name", PropertyLabel = "placeName" },
            new() { ColumnType = PropertyType.Normal, PropertyName = "place_id", PropertyLabel = "placeId" }
        };

        var result = OutputMapping.ConvertFromBson(doc, maps);

        result["placeName"]!.GetValue<string>().Should().Be("Botanic Garden");
        result["placeId"]!.GetValue<string>().Should().Be("42");
    }

    [Fact]
    public void ConvertFromBson_skips_missing_fields()
    {
        var doc = new BsonDocument { { "exists", "value" } };
        var maps = new List<PropertyMapping>
        {
            new() { ColumnType = PropertyType.Normal, PropertyName = "missing", PropertyLabel = "missingLabel" }
        };

        var result = OutputMapping.ConvertFromBson(doc, maps);

        result.ContainsKey("missingLabel").Should().BeFalse();
    }

    [Fact]
    public void ConvertFromBson_maps_object_fields_with_nested_content()
    {
        var geometry = new BsonDocument
        {
            { "type", "Polygon" },
            {
                "coordinates", new BsonArray
                {
                    new BsonArray
                    {
                        new BsonArray { 115.1, -8.2 },
                        new BsonArray { 115.2, -8.3 }
                    }
                }
            }
        };
        var doc = new BsonDocument { { "geometry", geometry } };
        var maps = new List<PropertyMapping>
        {
            new() { ColumnType = PropertyType.Object, PropertyName = "geometry", PropertyLabel = "geometry" }
        };

        var result = OutputMapping.ConvertFromBson(doc, maps);

        result["geometry"]!["type"]!.GetValue<string>().Should().Be("Polygon");
        result["geometry"]!["coordinates"]![0]![0]![0]!.GetValue<double>().Should().Be(115.1);
        result["geometry"]!["coordinates"]![0]![1]![1]!.GetValue<double>().Should().Be(-8.3);
    }
}