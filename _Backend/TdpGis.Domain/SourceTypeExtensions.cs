namespace TdpGis.Domain;

public static class SourceTypeExtensions
{
    public static string ToDisplayName(this SourceType type) => type switch
    {
        SourceType.Mongodb => "MongoDB",
        SourceType.Postgres => "PostgreSQL",
        SourceType.SqlServer => "SQL Server",
        _ => type.ToString()
    };

    public static bool IsRelational(this SourceType type) =>
        type is SourceType.Postgres or SourceType.SqlServer;
}
