using TdpGis.Domain;

namespace TdpGis.Infrastructure.Sql;

internal static class SqlIdentifiers
{
    public static (string Schema, string Name) SplitTableName(string tableName, SourceType databaseType)
    {
        var trimmed = tableName.Trim().Trim('[', ']');
        var dot = trimmed.IndexOf('.');
        if (dot <= 0 || dot == trimmed.Length - 1)
        {
            var defaultSchema = databaseType == SourceType.SqlServer ? "dbo" : "public";
            return (defaultSchema, trimmed);
        }

        return (trimmed[..dot].Trim('[', ']'), trimmed[(dot + 1)..].Trim('[', ']'));
    }

    public static string Quote(SourceType databaseType, string identifier)
    {
        return databaseType == SourceType.SqlServer ? QuoteSqlServer(identifier) : QuotePostgres(identifier);
    }

    public static string QuotePostgres(string identifier)
    {
        return $"\"{identifier.Replace("\"", "\"\"")}\"";
    }

    public static string QuoteSqlServer(string identifier)
    {
        return $"[{identifier.Replace("]", "]]")}]";
    }

    public static string EscapeLikePattern(string value)
    {
        return value
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("%", "\\%", StringComparison.Ordinal)
            .Replace("_", "\\_", StringComparison.Ordinal);
    }
}