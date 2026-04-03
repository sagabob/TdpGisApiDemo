using System.ComponentModel.DataAnnotations;
using TdpGis.Models;

namespace TdpGis.Endpoints.Models;

public class GisConnectionFormViewModel
{
    [Required]
    [Display(Name = "Saved MongoDB Connection")]
    public Guid? DataSourceId { get; set; }

    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [StringLength(500)]
    public string Description { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string Entity { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string EntityLabel { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string QueryField { get; set; } = string.Empty;

    [Required]
    public GeometryType GeometryType { get; set; } = GeometryType.MultiPolygon;

    [Display(Name = "Property Mappings")]
    public string PropertyMappingsText { get; set; } = string.Empty;
}

public class MongoConnectionFormViewModel
{
    [Required]
    [Display(Name = "MongoDB Connection String")]
    public string ConnectionString { get; set; } = string.Empty;
}

public class GisConnectionPageViewModel
{
    public MongoConnectionFormViewModel MongoForm { get; set; } = new();

    public GisConnectionFormViewModel Form { get; set; } = new();

    public IReadOnlyList<GisConnection> ExistingConnections { get; set; } = [];

    public IReadOnlyList<DataSourceSetting> SavedMongoConnections { get; set; } = [];
}
