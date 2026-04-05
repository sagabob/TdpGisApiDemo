namespace TdpGis.Infrastructure.Persistence;

/// <summary>
///     Npgsql maps PostgreSQL <c>timestamp with time zone</c> to .NET <see cref="DateTime" /> and requires UTC when
///     writing. Values from JSON, forms, or some readers may be <see cref="DateTimeKind.Unspecified" />.
/// </summary>
public static class DateTimeUtcForPostgreSql
{
    public static DateTime ToUtc(DateTime value)
    {
        var utc = value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            DateTimeKind.Unspecified => DateTime.SpecifyKind(value, DateTimeKind.Local).ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Local).ToUniversalTime()
        };
        return DateTime.SpecifyKind(utc, DateTimeKind.Utc);
    }

    /// <summary>Normalize values read from the database to UTC kind for consistent in-memory use.</summary>
    public static DateTime FromStore(DateTime value)
    {
        return value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
        };
    }
}
