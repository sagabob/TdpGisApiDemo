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

    public Guid? GisWorkspaceId { get; set; }

    public GisWorkspace? GisWorkspace { get; set; }

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

public class GisWorkspace
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public ICollection<GisConnection> Entities { get; set; } = [];

    public ICollection<GisWorkspaceAccessToken> AccessTokens { get; set; } = [];
}

public class GisWorkspaceAccessToken
{
    public Guid Id { get; set; }

    public Guid GisWorkspaceId { get; set; }

    public required string Name { get; set; }

    /// <summary>
    ///     Opaque secret (e.g. Base64Url random bytes); unique when used for lookup.
    /// </summary>
    public required string AccessToken { get; set; }

    public DateTime ExpiredDateTime { get; set; }

    public bool IsActive { get; set; }

    public bool IsPublic { get; set; }

    public required GisWorkspace GisWorkspace { get; set; }
}