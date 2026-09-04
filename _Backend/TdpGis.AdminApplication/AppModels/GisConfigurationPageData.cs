using TdpGis.Domain;

namespace TdpGis.AdminApplication.AppModels;

public sealed class GisConfigurationPageData
{
    public required IReadOnlyList<GisConnection> ExistingConnections { get; init; }

    public required IReadOnlyList<DataSourceSetting> SavedDataSources { get; init; }

    public required IReadOnlyList<GisWorkspace> Workspaces { get; init; }
}
