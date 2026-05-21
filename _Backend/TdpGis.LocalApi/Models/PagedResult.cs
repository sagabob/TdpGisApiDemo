namespace TdpGis.LocalApi.Models;

public class PagedResult<T>
{
    public IReadOnlyList<T> Items { get; init; } = [];
    public long TotalCount { get; set; }
    public int Page { get; set; }

    public int PageSize { get; set; }
}