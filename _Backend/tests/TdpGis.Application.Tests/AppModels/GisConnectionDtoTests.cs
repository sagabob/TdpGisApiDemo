using FluentAssertions;
using TdpGis.Application.AppModels;
using TdpGis.Domain;
using Xunit;

namespace TdpGis.Application.Tests.AppModels;

public class GisConnectionDtoTests
{
    [Fact]
    public void NewDto_ShouldDefaultDescriptionToEmptyString()
    {
        var dto = new GisConnectionDto
        {
            Id = Guid.NewGuid(),
            Name = "roads",
            Entity = "roads_collection",
            GeometryType = GeometryType.MultiPolygon,
            QueryField = "name",
            PropertyMappings =
            [
                new PropertyMapping
                {
                    Id = Guid.NewGuid(),
                    ColumnType = PropertyType.Normal,
                    PropertyName = "name",
                    PropertyLabel = "Name"
                }
            ],
            EntityLabel = "Roads"
        };

        dto.Description.Should().BeEmpty();
    }

    [Fact]
    public void Dto_ShouldStoreAssignedValues()
    {
        var id = Guid.NewGuid();
        var mappings = new List<PropertyMapping>
        {
            new()
            {
                Id = Guid.NewGuid(),
                ColumnType = PropertyType.Object,
                PropertyName = "metadata",
                PropertyLabel = "Metadata"
            }
        };

        var dto = new GisConnectionDto
        {
            Id = id,
            Name = "buildings",
            Entity = "buildings_collection",
            GeometryType = GeometryType.MultiPoint,
            QueryField = "code",
            PropertyMappings = mappings,
            EntityLabel = "Buildings",
            Description = "Building entities"
        };

        dto.Id.Should().Be(id);
        dto.Name.Should().Be("buildings");
        dto.Entity.Should().Be("buildings_collection");
        dto.GeometryType.Should().Be(GeometryType.MultiPoint);
        dto.QueryField.Should().Be("code");
        dto.PropertyMappings.Should().BeSameAs(mappings);
        dto.EntityLabel.Should().Be("Buildings");
        dto.Description.Should().Be("Building entities");
    }
}