using TdpGis.AdminApplication.Abstractions;
using TdpGis.AdminApplication.AppModels;
using TdpGis.Domain;

namespace TdpGis.AdminApplication.Services;

public sealed class GisAdminAppService(
    IGisConfigurationRepository repository,
    IMongoMetadataProvider mongo,
    ISqlMetadataProvider sql) : IGisAdminAppService
{
    public GisConfigurationPageData GetConfigurationPageData()
    {
        return new GisConfigurationPageData
        {
            ExistingConnections = repository.GetAllConnections(),
            SavedDataSources = repository.GetDataSources(),
            Workspaces = repository.GetAllWorkspaces()
        };
    }

    public GisConnection? GetGisConnectionById(Guid id)
    {
        return repository.GetConnectionById(id);
    }

    public void MapGisConnectionToForm(GisConnectionFormState form, GisConnection c)
    {
        form.GisConnectionId = c.Id;
        form.DataSourceId = c.DataSource.Id;
        form.Name = c.Name;
        form.Description = c.Description;
        form.Entity = c.Entity;
        form.EntityLabel = c.EntityLabel;
        form.QueryField = c.QueryField;
        form.GeometryType = c.GeometryType;
        form.GisWorkspaceId = c.GisWorkspaceId;
        form.PropertyMappingsText = BuildPropertyMappingsText(c);
    }

    public async Task<FormActionResult> SaveDataSourceAsync(SourceType databaseType, string connectionString,
        CancellationToken cancellationToken = default)
    {
        var result = new FormActionResult();
        var validation = await ValidateDataSourceConnectionAsync(databaseType, connectionString, cancellationToken);
        if (!validation.Ok)
        {
            result.AddFieldError("ConnectionString", validation.Message ?? "Connection validation failed.");
            return result;
        }

        await repository.CreateDataSourceAsync(databaseType, connectionString, cancellationToken);
        result.SuccessMessage = $"Saved {databaseType.ToDisplayName()} connection.";
        return result;
    }

    public async Task<FormActionResult> SaveGisConnectionAsync(SaveGisConnectionInput input,
        CancellationToken cancellationToken = default)
    {
        var result = new FormActionResult();

        if (!input.DataSourceId.HasValue)
            result.AddFieldError(nameof(SaveGisConnectionInput.DataSourceId),
                "Please select a saved data source connection.");

        if (string.IsNullOrWhiteSpace(input.Entity))
            result.AddFieldError(nameof(SaveGisConnectionInput.Entity), "Please select a collection or table.");

        DataSourceSetting? dataSource = null;
        if (input.DataSourceId.HasValue)
        {
            dataSource = repository.GetDataSourceById(input.DataSourceId.Value);
            if (dataSource is null)
                result.AddFieldError(nameof(SaveGisConnectionInput.DataSourceId),
                    "Selected data source connection was not found.");
        }

        if (dataSource is not null)
        {
            var probeError = await ProbeEntityAsync(dataSource, input.Entity, cancellationToken);
            if (probeError is not null)
                result.AddFieldError(nameof(SaveGisConnectionInput.DataSourceId), probeError);
        }

        var propertyMappings = ParseMappings(input.PropertyMappingsText);
        if (propertyMappings.Count == 0)
            result.AddFieldError(nameof(SaveGisConnectionInput.PropertyMappingsText),
                "At least one property mapping is required.");

        if (input.GisWorkspaceId is { } wid && wid != Guid.Empty)
        {
            var ws = repository.GetWorkspaceById(wid);
            if (ws is null)
                result.AddFieldError(nameof(SaveGisConnectionInput.GisWorkspaceId),
                    "Selected workspace was not found.");
        }

        var nameTrimmed = input.Name.Trim();
        if (repository.GisConnectionNameExists(nameTrimmed, input.GisConnectionId))
            result.AddFieldError(nameof(SaveGisConnectionInput.Name), "Another GIS connection already uses this name.");

        if (!result.IsSuccess) return result;

        Guid? workspaceFk = input.GisWorkspaceId is { } w && w != Guid.Empty ? w : null;
        dataSource = repository.GetDataSourceById(input.DataSourceId!.Value)!;

        if (input.GisConnectionId is { } editId && editId != Guid.Empty)
            try
            {
                var updated = await repository.UpdateConnectionAsync(
                    editId,
                    input.DataSourceId.Value,
                    nameTrimmed,
                    input.Description ?? string.Empty,
                    input.Entity.Trim(),
                    input.EntityLabel.Trim(),
                    input.QueryField.Trim(),
                    input.GeometryType,
                    workspaceFk,
                    propertyMappings,
                    cancellationToken);
                if (updated is null)
                {
                    result.AddFieldError(nameof(SaveGisConnectionInput.GisConnectionId),
                        "GIS connection was not found.");
                    return result;
                }

                result.SuccessMessage = $"Updated query entity '{updated.Name}'.";
                result.RedirectGisEditId = editId;
                return result;
            }
            catch (InvalidOperationException ex)
            {
                result.AddFieldError(nameof(SaveGisConnectionInput.DataSourceId), ex.Message);
                return result;
            }

        var connection = new GisConnection
        {
            Id = Guid.NewGuid(),
            Name = nameTrimmed,
            Description = (input.Description ?? string.Empty).Trim(),
            Entity = input.Entity.Trim(),
            EntityLabel = input.EntityLabel.Trim(),
            QueryField = input.QueryField.Trim(),
            GeometryType = input.GeometryType,
            PropertyMappings = propertyMappings,
            GisWorkspaceId = workspaceFk,
            GisWorkspace = null,
            DataSourceId = dataSource.Id,
            DataSource = dataSource
        };

        await repository.CreateConnectionAsync(connection, cancellationToken);
        result.SuccessMessage = $"Created query entity '{connection.Name}'.";
        return result;
    }

    public async Task<FormActionResult> AssignEntitiesToWorkspaceAsync(AssignEntitiesInput input,
        CancellationToken cancellationToken = default)
    {
        var result = new FormActionResult();
        var ids = input.SelectedConnectionIds ?? [];
        if (ids.Count == 0)
            result.AddFieldError(nameof(AssignEntitiesInput.SelectedConnectionIds),
                "Select at least one GIS entity to add to the workspace.");

        if (!result.IsSuccess) return result;

        try
        {
            var updated = await repository.SetConnectionsWorkspaceAsync(input.WorkspaceId, ids, cancellationToken);
            result.SuccessMessage = updated == 1
                ? "Assigned 1 GIS entity to the workspace."
                : $"Assigned {updated} GIS entities to the workspace.";
            return result;
        }
        catch (InvalidOperationException ex)
        {
            result.AddFieldError(nameof(AssignEntitiesInput.WorkspaceId), ex.Message);
            return result;
        }
    }

    public async Task<FormActionResult> SaveWorkspaceAsync(SaveWorkspaceInput input,
        CancellationToken cancellationToken = default)
    {
        var result = new FormActionResult();
        var name = input.Name.Trim();

        if (!input.WorkspaceId.HasValue || input.WorkspaceId == Guid.Empty)
        {
            await repository.CreateWorkspaceAsync(name, cancellationToken);
            result.SuccessMessage = $"Created workspace '{name}'.";
            return result;
        }

        var updated = await repository.UpdateWorkspaceAsync(input.WorkspaceId.Value, name, cancellationToken);
        if (updated is null)
        {
            result.AddFieldError(nameof(SaveWorkspaceInput.WorkspaceId), "Workspace was not found.");
            return result;
        }

        result.SuccessMessage = $"Updated workspace '{updated.Name}'.";
        return result;
    }

    public async Task<FormActionResult> CreateWorkspaceAccessTokenAsync(CreateAccessTokenInput input,
        CancellationToken cancellationToken = default)
    {
        var result = new FormActionResult();
        try
        {
            var token = await repository.CreateWorkspaceAccessTokenAsync(
                input.WorkspaceId,
                input.Name,
                input.ExpiredDateTime,
                input.IsActive,
                input.IsPublic,
                cancellationToken);
            result.SuccessMessage = "Access token created. Copy the secret below; it cannot be retrieved again.";
            result.CreatedAccessTokenPlain = token.AccessToken;
            return result;
        }
        catch (InvalidOperationException ex)
        {
            result.AddFieldError(nameof(CreateAccessTokenInput.WorkspaceId), ex.Message);
            return result;
        }
    }

    public async Task<FormActionResult> UpdateWorkspaceAccessTokenAsync(UpdateAccessTokenInput input,
        CancellationToken cancellationToken = default)
    {
        var result = new FormActionResult();
        try
        {
            var updated = await repository.UpdateWorkspaceAccessTokenAsync(
                input.TokenId,
                input.GisWorkspaceId,
                input.Name,
                input.ExpiredDateTime,
                input.IsActive,
                input.IsPublic,
                cancellationToken);
            if (updated is null)
            {
                result.ModelOnlyError = "Access token was not found.";
                return result;
            }
        }
        catch (InvalidOperationException ex)
        {
            result.AddFieldError(nameof(UpdateAccessTokenInput.GisWorkspaceId), ex.Message);
            return result;
        }

        result.SuccessMessage = "Access token settings updated.";
        return result;
    }

    public async Task<DataSourceValidationApiResponse> ValidateDataSourceConnectionAsync(SourceType databaseType,
        string connectionString, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            return new DataSourceValidationApiResponse(false, "Connection string is required.", null, null);

        if (databaseType == SourceType.Mongodb)
        {
            var probe = await mongo.ProbeConnectionAsync(connectionString, null, cancellationToken);
            if (!probe.IsValid)
                return new DataSourceValidationApiResponse(false, probe.ErrorMessage, null, null);

            return new DataSourceValidationApiResponse(true, null, probe.DatabaseName, probe.Collections);
        }

        if (databaseType.IsRelational())
        {
            var probe = await sql.ProbeConnectionAsync(databaseType, connectionString, null, cancellationToken);
            if (!probe.IsValid)
                return new DataSourceValidationApiResponse(false, probe.ErrorMessage, null, null);

            return new DataSourceValidationApiResponse(true, null, probe.DatabaseName, probe.Tables);
        }

        return new DataSourceValidationApiResponse(false, $"Unsupported database type '{databaseType}'.", null, null);
    }

    public async Task<CollectionsApiResponse> GetCollectionsForDataSourceAsync(Guid dataSourceId,
        CancellationToken cancellationToken = default)
    {
        var dataSource = repository.GetDataSourceById(dataSourceId);
        if (dataSource is null)
            return new CollectionsApiResponse(false, "Saved connection not found.", null);

        try
        {
            if (dataSource.DatabaseType == SourceType.Mongodb)
            {
                var collections = await mongo.ListCollectionNamesAsync(dataSource.ConnectionString, cancellationToken);
                return new CollectionsApiResponse(true, null, collections);
            }

            if (dataSource.DatabaseType.IsRelational())
            {
                var tables = await sql.ListTableNamesAsync(dataSource.DatabaseType, dataSource.ConnectionString,
                    cancellationToken);
                return new CollectionsApiResponse(true, null, tables);
            }

            return new CollectionsApiResponse(false, $"Unsupported database type '{dataSource.DatabaseType}'.", null);
        }
        catch (Exception ex)
        {
            return new CollectionsApiResponse(false, $"Connection failed: {ex.Message}", null);
        }
    }

    public async Task<DataSourceSampleApiResponse> GetSampleAsync(Guid dataSourceId, string entityName,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(entityName))
            return new DataSourceSampleApiResponse(false, "Collection or table is required.", false, [], "{}", null);

        var dataSource = repository.GetDataSourceById(dataSourceId);
        if (dataSource is null)
            return new DataSourceSampleApiResponse(false, "Saved connection not found.", false, [], "{}", null);

        try
        {
            if (dataSource.DatabaseType == SourceType.Mongodb)
            {
                var sample = await mongo.GetSampleDocumentAsync(dataSource.ConnectionString, entityName.Trim(),
                    cancellationToken);
                return ToSampleResponse(sample.HasSample, sample.Fields, sample.SampleJson);
            }

            if (dataSource.DatabaseType.IsRelational())
            {
                var sample = await sql.GetSampleRowAsync(dataSource.DatabaseType, dataSource.ConnectionString,
                    entityName.Trim(), cancellationToken);
                return ToSampleResponse(sample.HasSample, sample.Fields, sample.SampleJson);
            }

            return new DataSourceSampleApiResponse(false, $"Unsupported database type '{dataSource.DatabaseType}'.",
                false, [], "{}", null);
        }
        catch (Exception ex)
        {
            return new DataSourceSampleApiResponse(false, $"Could not read sample: {ex.Message}", false, [], "{}",
                null);
        }
    }

    private static DataSourceSampleApiResponse ToSampleResponse(bool hasSample, IReadOnlyList<string> fields,
        string sampleJson)
    {
        var suggested = GeometryTypeDetector.DetectFromSampleJson(sampleJson)?.ToString();
        return new DataSourceSampleApiResponse(true, null, hasSample, fields, sampleJson, suggested);
    }

    private async Task<string?> ProbeEntityAsync(DataSourceSetting dataSource, string? entityName,
        CancellationToken cancellationToken)
    {
        if (dataSource.DatabaseType == SourceType.Mongodb)
        {
            var probe = await mongo.ProbeConnectionAsync(dataSource.ConnectionString, entityName, cancellationToken);
            return probe.IsValid ? null : probe.ErrorMessage;
        }

        if (dataSource.DatabaseType.IsRelational())
        {
            var probe = await sql.ProbeConnectionAsync(dataSource.DatabaseType, dataSource.ConnectionString, entityName,
                cancellationToken);
            return probe.IsValid ? null : probe.ErrorMessage;
        }

        return $"Unsupported database type '{dataSource.DatabaseType}'.";
    }

    private static string BuildPropertyMappingsText(GisConnection c)
    {
        return string.Join(Environment.NewLine,
            c.PropertyMappings.OrderBy(m => m.PropertyName)
                .Select(m => $"{m.PropertyName}|{m.PropertyLabel}|{m.ColumnType}"));
    }

    private static List<PropertyMapping> ParseMappings(string mappingsText)
    {
        var lines = mappingsText
            .Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var mappings = new List<PropertyMapping>();
        foreach (var line in lines)
        {
            var parts = line.Split('|', 3, StringSplitOptions.TrimEntries);
            if (parts.Length < 2 || string.IsNullOrWhiteSpace(parts[0]) ||
                string.IsNullOrWhiteSpace(parts[1])) continue;

            var columnType = PropertyType.Normal;
            if (parts.Length == 3 && Enum.TryParse<PropertyType>(parts[2], true, out var parsedType))
                columnType = parsedType;

            mappings.Add(new PropertyMapping
            {
                Id = Guid.NewGuid(),
                PropertyName = parts[0],
                PropertyLabel = parts[1],
                ColumnType = columnType
            });
        }

        return mappings;
    }
}
