namespace TdpGis.Models;

public class GisConnection
{
    public Guid Id { get; set; }

    public required string Name { get; set; }

    public string Description { get; set; } = string.Empty;

    public required GeometryType GeometryType { get; set; }

    public required string QueryField { get; set; }

    public required List<PropertyMapping> PropertyMappings { get; set; }

    public required string Entity { get; set; }

    public required string EntityLabel { get; set; }

    public required DataSourceSetting DataSource { get; set; }
}

public enum GeometryType
{
    MultiPoint,
    MultiPolygon
}

public enum PropertyType
{
    Normal,
    Object
}

public class PropertyMapping
{
    public Guid Id { get; set; }
    public PropertyType ColumnType { get; set; }
    public required string PropertyName { get; set; }
    public required string PropertyLabel { get; set; }
}

public class DataSourceSetting
{
    public Guid Id { get; set; }
    public required string ConnectionString { get; set; }
    public SourceType DatabaseType { get; set; }
}

public enum SourceType
{
    Mongodb,
    Postgres
}