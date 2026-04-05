using TdpGis.Domain;

namespace TdpGis.Application.AppModels;

public class GisConnectionDto
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public required GeometryType GeometryType { get; set; }
    public required string QueryField { get; set; }
    public required List<PropertyMapping> PropertyMappings { get; set; }
    public required string EntityLabel { get; set; }
    public string Description { get; set; } = string.Empty;
}
