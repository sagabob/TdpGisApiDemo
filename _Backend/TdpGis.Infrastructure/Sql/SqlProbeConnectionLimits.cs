namespace TdpGis.Infrastructure.Sql;

/// <summary>
///     Connection-pool caps for GIS metadata probes and the app metadata database.
///     Hosted Postgres (e.g. Aiven) often allows only ~20 slots; Npgsql defaults to 100.
/// </summary>
internal static class SqlProbeConnectionLimits
{
    public const int GisMetadataMaxPoolSize = 3;
    public const int GisMetadataTimeoutSeconds = 15;
    public const int AppDbMaxPoolSize = 10;
}