using System.Text.Json.Nodes;
using FluentAssertions;
using MongoDB.Bson;
using TdpGis.Domain;
using TdpGis.Infrastructure.Helpers;
using Xunit;

namespace TdpGis.Infrastructure.Tests.Helpers;

public class OutputMappingTests
{
    [Fact]
    public void ConvertFromBson_ShouldThrow_WhenDocIsNull()
    {
        Action act = () => OutputMapping.ConvertFromBson(null!, []);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ConvertFromBson_ShouldThrow_WhenMapsIsNull()
    {
        Action act = () => OutputMapping.ConvertFromBson(new BsonDocument(), null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ConvertFromBson_ShouldMapNormalColumnsAsStrings_AndObjectColumnsAsStructuredJson()
    {
        var doc = new BsonDocument
        {
            ["name"] = "London",
            ["count"] = 3,
            ["meta"] = new BsonDocument
            {
                ["enabled"] = true,
                ["tags"] = new BsonArray { "a", "b" }
            },
            ["nullable"] = BsonNull.Value
        };

        var maps = new List<PropertyMapping>
        {
            new() { PropertyName = "name", PropertyLabel = "Name", ColumnType = PropertyType.Normal },
            new() { PropertyName = "count", PropertyLabel = "Count", ColumnType = PropertyType.Normal },
            new() { PropertyName = "meta", PropertyLabel = "Meta", ColumnType = PropertyType.Object },
            new() { PropertyName = "nullable", PropertyLabel = "Nullable", ColumnType = PropertyType.Object },
            new() { PropertyName = "missing", PropertyLabel = "Missing", ColumnType = PropertyType.Normal }
        };

        var result = OutputMapping.ConvertFromBson(doc, maps);

        result["Name"]!.GetValue<string>().Should().Be("London");
        result["Count"]!.GetValue<string>().Should().Be("3");

        var meta = result["Meta"].Should().BeOfType<JsonObject>().Subject;
        meta["enabled"]!.GetValue<bool>().Should().BeTrue();
        meta["tags"].Should().BeOfType<JsonArray>().Subject.Count.Should().Be(2);

        result.ContainsKey("Nullable").Should().BeTrue();
        result["Nullable"].Should().BeNull();
        result.ContainsKey("Missing").Should().BeFalse();
    }

    [Fact]
    public void ConvertFromRow_ShouldMapNormalAndObjectColumns()
    {
        var row = new Dictionary<string, object?>
        {
            ["name"] = "London",
            ["count"] = 3,
            ["meta"] = """{"enabled":true}""",
            ["nullable"] = null
        };

        var maps = new List<PropertyMapping>
        {
            new() { PropertyName = "name", PropertyLabel = "Name", ColumnType = PropertyType.Normal },
            new() { PropertyName = "count", PropertyLabel = "Count", ColumnType = PropertyType.Normal },
            new() { PropertyName = "meta", PropertyLabel = "Meta", ColumnType = PropertyType.Object },
            new() { PropertyName = "nullable", PropertyLabel = "Nullable", ColumnType = PropertyType.Object },
            new() { PropertyName = "missing", PropertyLabel = "Missing", ColumnType = PropertyType.Normal }
        };

        var result = OutputMapping.ConvertFromRow(row, maps);

        result["Name"]!.GetValue<string>().Should().Be("London");
        result["Count"]!.GetValue<string>().Should().Be("3");
        result["Meta"].Should().BeOfType<JsonObject>().Subject["enabled"]!.GetValue<bool>().Should().BeTrue();
        result.ContainsKey("Nullable").Should().BeTrue();
        result["Nullable"].Should().BeNull();
        result.ContainsKey("Missing").Should().BeFalse();
    }
}