namespace TdpGis.LocalApi.Models;

public class SearchFeature
{
    public required string ConnectionString { get; set; }
    public required string CollectionName { get; set; }
    public required string DatabaseName { get; set; }
    public required string SearchField { get; set; }
    public string[] IncludeFields { get; set; } = [];
}