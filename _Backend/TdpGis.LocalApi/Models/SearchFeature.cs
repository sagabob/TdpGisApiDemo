namespace TdpGis.LocalApi.Models;

public class SearchFeature
{
    // Parameterless constructor required by ConfigurationBinder / IOptions binding
    public SearchFeature()
    {
    }

    // Optional convenience constructor
    public SearchFeature(string connectionString, string collectionName, string databaseName, string searchField)
    {
        ConnectionString = connectionString;
        CollectionName = collectionName;
        DatabaseName = databaseName;
        SearchField = searchField;
    }

    public required string ConnectionString { get; set; }
    public required string CollectionName { get; set; }
    public required string DatabaseName { get; set; }
    public required string SearchField { get; set; }
}