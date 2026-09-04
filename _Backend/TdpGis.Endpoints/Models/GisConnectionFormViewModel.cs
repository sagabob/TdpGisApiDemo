using System.ComponentModel.DataAnnotations;
using TdpGis.Domain;

namespace TdpGis.Endpoints.Models;

public class GisConnectionFormViewModel
{
    /// <summary>When set, the form updates an existing GIS connection instead of creating one.</summary>
    public Guid? GisConnectionId { get; set; }

    [Required]
    [Display(Name = "Saved data source")]
    public Guid? DataSourceId { get; set; }

    [Required] [StringLength(100)] public string Name { get; set; } = string.Empty;

    [StringLength(500)] public string Description { get; set; } = string.Empty;

    [Required] [StringLength(200)] public string Entity { get; set; } = string.Empty;

    [Required] [StringLength(100)] public string EntityLabel { get; set; } = string.Empty;

    [Required] [StringLength(100)] public string QueryField { get; set; } = string.Empty;

    [Required] public GeometryType GeometryType { get; set; } = GeometryType.Point;

    [Display(Name = "Workspace")] public Guid? GisWorkspaceId { get; set; }

    [Display(Name = "Property Mappings")] public string PropertyMappingsText { get; set; } = string.Empty;
}

public class DataSourceConnectionFormViewModel
{
    [Required]
    [Display(Name = "Database type")]
    public SourceType DatabaseType { get; set; } = SourceType.Mongodb;

    [Required]
    [Display(Name = "Connection string")]
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

    [Required] [Display(Name = "Expires")] public DateTime ExpiredDateTime { get; set; } = DateTime.Today.AddDays(90);

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
    public DataSourceConnectionFormViewModel DataSourceForm { get; set; } = new();

    public GisConnectionFormViewModel Form { get; set; } = new();

    public WorkspaceFormViewModel WorkspaceForm { get; set; } = new();

    public AccessTokenFormViewModel AccessTokenForm { get; set; } = new();

    public AssignEntitiesWorkspaceFormViewModel AssignEntitiesForm { get; set; } = new();

    public IReadOnlyList<GisConnection> ExistingConnections { get; set; } = [];

    public IReadOnlyList<DataSourceSetting> SavedDataSources { get; set; } = [];

    public IReadOnlyList<GisWorkspace> Workspaces { get; set; } = [];

    /// <summary>When set, row fields use these values (e.g. after a failed update post).</summary>
    public UpdateWorkspaceAccessTokenFormViewModel? UpdateTokenForm { get; set; }

    /// <summary>True when the signed-in user has the Entra app role configured as <c>AzureAd:AdminAppRole</c>.</summary>
    public bool CanManageConfiguration { get; set; }
}
