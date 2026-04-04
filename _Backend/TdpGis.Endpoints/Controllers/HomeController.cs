using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Driver;
using TdpGis.Application.DatabaseService;
using TdpGis.Endpoints.Models;
using TdpGis.Models;

namespace TdpGis.Endpoints.Controllers;

public class HomeController(IGisDbService gisDbService) : Controller
{
    public IActionResult Index()
    {
        return View("Project");
    }

    public IActionResult Configuration(Guid? gisEdit)
    {
        var model = BuildPageModel();
        if (gisEdit.HasValue)
        {
            var conn = gisDbService.GetConnectionById(gisEdit.Value);
            if (conn is not null) MapGisConnectionToForm(model.Form, conn);
        }

        return View("Index", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveMongoConnection(
        [Bind(Prefix = "MongoForm")] MongoConnectionFormViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            var pageModel = BuildPageModel();
            pageModel.MongoForm = model;
            return View("Index", pageModel);
        }

        var mongoValidation = await ValidateMongoForSaveAsync(
            model.ConnectionString,
            null,
            cancellationToken);
        if (!mongoValidation.IsValid)
        {
            ModelState.AddModelError(nameof(model.ConnectionString), mongoValidation.ErrorMessage);
            var pageModel = BuildPageModel();
            pageModel.MongoForm = model;
            return View("Index", pageModel);
        }

        await gisDbService.CreateMongoDataSourceAsync(model.ConnectionString, cancellationToken);
        TempData["SuccessMessage"] = "Saved MongoDB connection.";
        return RedirectToAction(nameof(Configuration));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveGisConnection([Bind(Prefix = "Form")] GisConnectionFormViewModel model,
        CancellationToken cancellationToken)
    {
        if (!model.DataSourceId.HasValue)
            ModelState.AddModelError(nameof(model.DataSourceId), "Please select a saved MongoDB connection.");

        if (string.IsNullOrWhiteSpace(model.Entity))
            ModelState.AddModelError(nameof(model.Entity), "Please select a collection.");

        var dataSource = model.DataSourceId.HasValue ? gisDbService.GetDataSourceById(model.DataSourceId.Value) : null;
        if (dataSource is null)
        {
            ModelState.AddModelError(nameof(model.DataSourceId), "Selected MongoDB connection was not found.");
        }
        else
        {
            var mongoValidation = await ValidateMongoForSaveAsync(
                dataSource.ConnectionString,
                model.Entity,
                cancellationToken);
            if (!mongoValidation.IsValid)
                ModelState.AddModelError(nameof(model.DataSourceId), mongoValidation.ErrorMessage);
        }

        var propertyMappings = ParseMappings(model.PropertyMappingsText);
        if (propertyMappings.Count == 0)
            ModelState.AddModelError(nameof(model.PropertyMappingsText), "At least one property mapping is required.");

        if (model.GisWorkspaceId.HasValue && model.GisWorkspaceId != Guid.Empty)
        {
            var ws = gisDbService.GetWorkspaceById(model.GisWorkspaceId.Value);
            if (ws is null) ModelState.AddModelError(nameof(model.GisWorkspaceId), "Selected workspace was not found.");
        }

        if (gisDbService.GisConnectionNameExists(model.Name.Trim(), model.GisConnectionId))
            ModelState.AddModelError(nameof(model.Name), "Another GIS connection already uses this name.");

        if (!ModelState.IsValid)
        {
            var pageModel = BuildPageModel();
            pageModel.Form = model;
            return View("Index", pageModel);
        }

        Guid? workspaceFk = model.GisWorkspaceId is { } wid && wid != Guid.Empty ? wid : null;

        if (model.GisConnectionId is { } editId && editId != Guid.Empty)
        {
            try
            {
                var updated = await gisDbService.UpdateConnectionAsync(
                    editId,
                    model.DataSourceId!.Value,
                    model.Name.Trim(),
                    model.Description ?? string.Empty,
                    model.Entity.Trim(),
                    model.EntityLabel.Trim(),
                    model.QueryField.Trim(),
                    model.GeometryType,
                    workspaceFk,
                    propertyMappings,
                    cancellationToken);
                if (updated is null)
                {
                    ModelState.AddModelError(nameof(model.GisConnectionId), "GIS connection was not found.");
                    var pageModel = BuildPageModel();
                    pageModel.Form = model;
                    return View("Index", pageModel);
                }

                TempData["SuccessMessage"] = $"Updated query entity '{updated.Name}'.";
                return RedirectToAction(nameof(Configuration), new { gisEdit = editId });
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError(nameof(model.DataSourceId), ex.Message);
                var pageModel = BuildPageModel();
                pageModel.Form = model;
                return View("Index", pageModel);
            }
        }

        var connection = new GisConnection
        {
            Id = Guid.NewGuid(),
            Name = model.Name.Trim(),
            Description = model.Description.Trim(),
            Entity = model.Entity.Trim(),
            EntityLabel = model.EntityLabel.Trim(),
            QueryField = model.QueryField.Trim(),
            GeometryType = model.GeometryType,
            PropertyMappings = propertyMappings,
            GisWorkspaceId = workspaceFk,
            GisWorkspace = null,
            DataSourceId = dataSource!.Id,
            DataSource = dataSource
        };

        await gisDbService.CreateConnectionAsync(connection, cancellationToken);
        TempData["SuccessMessage"] = $"Created query entity '{connection.Name}'.";
        return RedirectToAction(nameof(Configuration));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AssignEntitiesToWorkspace(
        [Bind(Prefix = "AssignEntitiesForm")] AssignEntitiesWorkspaceFormViewModel model,
        CancellationToken cancellationToken)
    {
        var ids = model.SelectedConnectionIds ?? [];
        if (ids.Count == 0)
            ModelState.AddModelError(
                nameof(model.SelectedConnectionIds),
                "Select at least one GIS entity to add to the workspace.");

        if (!ModelState.IsValid)
        {
            var pageModel = BuildPageModel();
            pageModel.AssignEntitiesForm = model;
            return View("Index", pageModel);
        }

        try
        {
            var updated = await gisDbService.SetConnectionsWorkspaceAsync(model.WorkspaceId, ids, cancellationToken);
            TempData["SuccessMessage"] = updated == 1
                ? "Assigned 1 GIS entity to the workspace."
                : $"Assigned {updated} GIS entities to the workspace.";
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(nameof(model.WorkspaceId), ex.Message);
            var pageModel = BuildPageModel();
            pageModel.AssignEntitiesForm = model;
            return View("Index", pageModel);
        }

        return RedirectToAction(nameof(Configuration));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveWorkspace([Bind(Prefix = "WorkspaceForm")] WorkspaceFormViewModel model,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            var pageModel = BuildPageModel();
            pageModel.WorkspaceForm = model;
            return View("Index", pageModel);
        }

        var name = model.Name.Trim();
        if (!model.WorkspaceId.HasValue || model.WorkspaceId == Guid.Empty)
        {
            await gisDbService.CreateWorkspaceAsync(name, cancellationToken);
            TempData["SuccessMessage"] = $"Created workspace '{name}'.";
        }
        else
        {
            var updated = await gisDbService.UpdateWorkspaceAsync(model.WorkspaceId.Value, name, cancellationToken);
            if (updated is null)
            {
                ModelState.AddModelError(nameof(model.WorkspaceId), "Workspace was not found.");
                var pageModel = BuildPageModel();
                pageModel.WorkspaceForm = model;
                return View("Index", pageModel);
            }

            TempData["SuccessMessage"] = $"Updated workspace '{updated.Name}'.";
        }

        return RedirectToAction(nameof(Configuration));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateWorkspaceAccessToken(
        [Bind(Prefix = "AccessTokenForm")] AccessTokenFormViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            var pageModel = BuildPageModel();
            pageModel.AccessTokenForm = model;
            return View("Index", pageModel);
        }

        try
        {
            var token = await gisDbService.CreateWorkspaceAccessTokenAsync(
                model.WorkspaceId,
                model.Name,
                model.ExpiredDateTime,
                model.IsActive,
                model.IsPublic,
                cancellationToken);
            TempData["SuccessMessage"] = "Access token created. Copy the secret below; it cannot be retrieved again.";
            TempData["CreatedAccessTokenPlain"] = token.AccessToken;
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(nameof(model.WorkspaceId), ex.Message);
            var pageModel = BuildPageModel();
            pageModel.AccessTokenForm = model;
            return View("Index", pageModel);
        }

        return RedirectToAction(nameof(Configuration));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateWorkspaceAccessToken(
        [Bind(Prefix = "UpdateToken")] UpdateWorkspaceAccessTokenFormViewModel model,
        CancellationToken cancellationToken)
    {
        ApplyUpdateTokenCheckboxesFromForm(Request.Form, model);

        if (!ModelState.IsValid)
        {
            var pageModel = BuildPageModel();
            pageModel.UpdateTokenForm = model;
            return View("Index", pageModel);
        }

        try
        {
            var updated = await gisDbService.UpdateWorkspaceAccessTokenAsync(
                model.TokenId,
                model.GisWorkspaceId,
                model.Name,
                model.ExpiredDateTime,
                model.IsActive,
                model.IsPublic,
                cancellationToken);
            if (updated is null)
            {
                ModelState.AddModelError(string.Empty, "Access token was not found.");
                var pageModel = BuildPageModel();
                pageModel.UpdateTokenForm = model;
                return View("Index", pageModel);
            }
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(nameof(model.GisWorkspaceId), ex.Message);
            var pageModel = BuildPageModel();
            pageModel.UpdateTokenForm = model;
            return View("Index", pageModel);
        }

        TempData["SuccessMessage"] = "Access token settings updated.";
        return RedirectToAction(nameof(Configuration));
    }

    [HttpPost]
    public async Task<IActionResult> ValidateMongoConnection([FromBody] MongoValidationRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.ConnectionString))
            return BadRequest(new { message = "Connection string is required." });

        var databaseName = GetDatabaseName(request.ConnectionString);
        if (string.IsNullOrWhiteSpace(databaseName))
            return BadRequest(new
                { message = "Database name is required in the MongoDB connection string (mongodb://.../<database>)." });

        try
        {
            var client = new MongoClient(request.ConnectionString.Trim());
            var database = client.GetDatabase(databaseName);
            var collections = await database.ListCollectionNames().ToListAsync(cancellationToken);

            return Ok(new
            {
                databaseName,
                collections
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = $"MongoDB connection failed: {ex.Message}" });
        }
    }

    [HttpPost]
    public async Task<IActionResult> GetCollectionsForSavedConnection([FromBody] SavedConnectionRequest request,
        CancellationToken cancellationToken)
    {
        var dataSource = gisDbService.GetDataSourceById(request.DataSourceId);
        if (dataSource is null) return BadRequest(new { message = "Saved connection not found." });

        try
        {
            var databaseName = GetDatabaseName(dataSource.ConnectionString);
            var client = new MongoClient(dataSource.ConnectionString);
            var database = client.GetDatabase(databaseName);
            var collections = await database.ListCollectionNames().ToListAsync(cancellationToken);
            return Ok(new { collections });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = $"MongoDB connection failed: {ex.Message}" });
        }
    }

    [HttpPost]
    public async Task<IActionResult> GetMongoSampleForSavedConnection([FromBody] SavedConnectionSampleRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.CollectionName))
            return BadRequest(new { message = "Collection is required." });

        var dataSource = gisDbService.GetDataSourceById(request.DataSourceId);
        if (dataSource is null) return BadRequest(new { message = "Saved connection not found." });

        try
        {
            var databaseName = GetDatabaseName(dataSource.ConnectionString);
            var client = new MongoClient(dataSource.ConnectionString);
            var database = client.GetDatabase(databaseName);
            var collection = database.GetCollection<BsonDocument>(request.CollectionName.Trim());
            var sample = await collection.Find(FilterDefinition<BsonDocument>.Empty).Limit(1)
                .FirstOrDefaultAsync(cancellationToken);

            if (sample is null)
                return Ok(new
                {
                    hasSample = false,
                    fields = Array.Empty<string>(),
                    sampleJson = "{}"
                });

            var fields = sample.Names.ToArray();
            return Ok(new
            {
                hasSample = true,
                fields,
                sampleJson = sample.ToJson(new JsonWriterSettings { Indent = true })
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = $"Could not read sample document: {ex.Message}" });
        }
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }

    private GisConnectionPageViewModel BuildPageModel()
    {
        return new GisConnectionPageViewModel
        {
            ExistingConnections = gisDbService.GetAllConnections(),
            SavedMongoConnections = gisDbService.GetMongoDataSources(),
            Workspaces = gisDbService.GetAllWorkspaces()
        };
    }

    private static void MapGisConnectionToForm(GisConnectionFormViewModel form, GisConnection c)
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

    private static string GetDatabaseName(string connectionString)
    {
        var mongoUrl = MongoUrl.Create(connectionString.Trim());
        return mongoUrl.DatabaseName ?? string.Empty;
    }

    private static async Task<(bool IsValid, string ErrorMessage)> ValidateMongoForSaveAsync(
        string connectionString,
        string? collectionName,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(connectionString)) return (false, "MongoDB connection string is required.");

        var resolvedDatabaseName = GetDatabaseName(connectionString);
        if (string.IsNullOrWhiteSpace(resolvedDatabaseName))
            return (false, "Database name is required in the MongoDB connection string (mongodb://.../<database>).");

        try
        {
            var client = new MongoClient(connectionString.Trim());
            var database = client.GetDatabase(resolvedDatabaseName);
            var collections = await database.ListCollectionNames().ToListAsync(cancellationToken);
            if (!string.IsNullOrWhiteSpace(collectionName) &&
                !collections.Contains(collectionName.Trim(), StringComparer.Ordinal))
                return (false, $"Collection '{collectionName}' was not found in database '{resolvedDatabaseName}'.");
        }
        catch (Exception ex)
        {
            return (false, $"MongoDB connection failed: {ex.Message}");
        }

        return (true, string.Empty);
    }

    private static void ApplyUpdateTokenCheckboxesFromForm(IFormCollection form,
        UpdateWorkspaceAccessTokenFormViewModel model)
    {
        model.IsActive = FormHasCheckboxTrue(form, "UpdateToken.IsActive");
        model.IsPublic = FormHasCheckboxTrue(form, "UpdateToken.IsPublic");
    }

    private static bool FormHasCheckboxTrue(IFormCollection form, string key)
    {
        var values = form[key];
        return values.Any(t => string.Equals(t, "true", StringComparison.Ordinal));
    }

    public sealed class MongoValidationRequest
    {
        public string ConnectionString { get; set; } = string.Empty;
    }

    public sealed class SavedConnectionRequest
    {
        public Guid DataSourceId { get; set; }
    }

    public sealed class SavedConnectionSampleRequest
    {
        public Guid DataSourceId { get; set; }
        public string CollectionName { get; set; } = string.Empty;
    }
}