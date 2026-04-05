using TdpGis.Domain;

namespace TdpGis.Application.Services;

public sealed class GisConfigurationPageData
{
    public required IReadOnlyList<GisConnection> ExistingConnections { get; init; }

    public required IReadOnlyList<DataSourceSetting> SavedMongoConnections { get; init; }

    public required IReadOnlyList<GisWorkspace> Workspaces { get; init; }
}
