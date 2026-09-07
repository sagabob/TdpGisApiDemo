namespace TdpGis.Application.UseCases.SearchGisEntity;

public static class SearchGisEntityLimits
{
    public const int DefaultMaxResults = 10;
    public const int AbsoluteMaxResults = 100;

    public static int Clamp(int? requested)
    {
        var value = requested ?? DefaultMaxResults;
        return value < 1 ? DefaultMaxResults : Math.Min(value, AbsoluteMaxResults);
    }
}