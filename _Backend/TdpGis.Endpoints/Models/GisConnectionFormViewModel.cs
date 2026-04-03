using System.ComponentModel.DataAnnotations;
using TdpGis.Models;

namespace TdpGis.Endpoints.Models;

public class GisConnectionFormViewModel
{
    [Required]
    [Display(Name = "Saved MongoDB Connection")]
    public Guid? DataSourceId { get; set; }

    [Required] [StringLength(100)] public string Name { get; set; } = string.Empty;

    [StringLength(500)] public string Description { get; set; } = string.Empty;

    [Required] [StringLength(100)] public string Entity { get; set; } = string.Empty;

    [Required] [StringLength(100)] public string EntityLabel { get; set; } = string.Empty;

    [Required] [StringLength(100)] public string QueryField { get; set; } = string.Empty;

    [Required] public GeometryType GeometryType { get; set; } = GeometryType.MultiPolygon;

    [Display(Name = "Workspace")] public Guid? GisWorkspaceId { get; set; }

    [Display(Name = "Property Mappings")] public string PropertyMappingsText { get; set; } = string.Empty;
}

public class MongoConnectionFormViewModel
{
    [Required]
    [Display(Name = "MongoDB Connection String")]
    public string ConnectionString { get; set; } = string.Empty;
}

public class WorkspaceFormViewModel
{
    public Guid? WorkspaceId { get; set; }

    [Required]
    [StringLength(200)]
    [Display(Name = "Workspace name")]
    public string Name { get; set; } = string.Empty;
}

public class AssignEntitiesWorkspaceFormViewModel
{
    [Required(ErrorMessage = "Select a workspace.")]
    [Display(Name = "Workspace")]
    public Guid WorkspaceId { get; set; }

    public List<Guid> SelectedConnectionIds { get; set; } = [];
}

public class AccessTokenFormViewModel
{
    [Required(ErrorMessage = "Select a workspace.")]
    [Display(Name = "Workspace")]
    public Guid WorkspaceId { get; set; }

    [Required(ErrorMessage = "Name is required.")]
    [StringLength(200)]
    [Display(Name = "Name")]
    public string Name { get; set; } = string.Empty;

    [Required] [Display(Name = "Expires")] public DateTime ExpiredDateTime { get; set; } = DateTime.UtcNow.AddDays(90);

    [Display(Name = "Active")] public bool IsActive { get; set; } = true;

    [Display(Name = "Public")] public bool IsPublic { get; set; }
}

public class UpdateWorkspaceAccessTokenFormViewModel
{
    [Required] public Guid TokenId { get; set; }

    [Required(ErrorMessage = "Select a workspace.")]
    [Display(Name = "Workspace")]
    public Guid GisWorkspaceId { get; set; }

    [Required(ErrorMessage = "Name is required.")]
    [StringLength(200)]
    [Display(Name = "Name")]
    public string Name { get; set; } = string.Empty;

    [Required] [Display(Name = "Expires")] public DateTime ExpiredDateTime { get; set; }

    public bool IsActive { get; set; }

    public bool IsPublic { get; set; }
}

public class GisConnectionPageViewModel
{
    public MongoConnectionFormViewModel MongoForm { get; set; } = new();

    public GisConnectionFormViewModel Form { get; set; } = new();

    public WorkspaceFormViewModel WorkspaceForm { get; set; } = new();

    public AccessTokenFormViewModel AccessTokenForm { get; set; } = new();

    public AssignEntitiesWorkspaceFormViewModel AssignEntitiesForm { get; set; } = new();

    public IReadOnlyList<GisConnection> ExistingConnections { get; set; } = [];

    public IReadOnlyList<DataSourceSetting> SavedMongoConnections { get; set; } = [];

    public IReadOnlyList<GisWorkspace> Workspaces { get; set; } = [];

    /// <summary>When set, row fields use these values (e.g. after a failed update post).</summary>
    public UpdateWorkspaceAccessTokenFormViewModel? UpdateTokenForm { get; set; }
}