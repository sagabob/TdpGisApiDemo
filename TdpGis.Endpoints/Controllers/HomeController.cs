using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Driver;
using System.Diagnostics;
using TdpGis.Application.DatabaseService;
using TdpGis.Endpoints.Models;
using TdpGis.Models;

namespace TdpGis.Endpoints.Controllers
{
    public class HomeController(IGisDbService gisDbService) : Controller
    {
        public IActionResult Index()
        {
            return View("Project");
        }

        public IActionResult Configuration()
        {
            var model = BuildPageModel();
            return View("Index", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveMongoConnection([Bind(Prefix = "MongoForm")] MongoConnectionFormViewModel model, CancellationToken cancellationToken)
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
        public async Task<IActionResult> SaveGisConnection([Bind(Prefix = "Form")] GisConnectionFormViewModel model, CancellationToken cancellationToken)
        {
            if (!model.DataSourceId.HasValue)
            {
                ModelState.AddModelError(nameof(model.DataSourceId), "Please select a saved MongoDB connection.");
            }

            if (string.IsNullOrWhiteSpace(model.Entity))
            {
                ModelState.AddModelError(nameof(model.Entity), "Please select a collection.");
            }

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
                {
                    ModelState.AddModelError(nameof(model.DataSourceId), mongoValidation.ErrorMessage);
                }
            }

            var propertyMappings = ParseMappings(model.PropertyMappingsText);
            if (propertyMappings.Count == 0)
            {
                ModelState.AddModelError(nameof(model.PropertyMappingsText), "At least one property mapping is required.");
            }

            if (!ModelState.IsValid)
            {
                var pageModel = BuildPageModel();
                pageModel.Form = model;
                return View("Index", pageModel);
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
                DataSource = dataSource!
            };

            await gisDbService.CreateConnectionAsync(connection, cancellationToken);
            TempData["SuccessMessage"] = $"Created query entity '{connection.Name}'.";
            return RedirectToAction(nameof(Configuration));
        }

        [HttpPost]
        public async Task<IActionResult> ValidateMongoConnection([FromBody] MongoValidationRequest request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.ConnectionString))
            {
                return BadRequest(new { message = "Connection string is required." });
            }

            var databaseName = GetDatabaseName(request.ConnectionString);
            if (string.IsNullOrWhiteSpace(databaseName))
            {
                return BadRequest(new { message = "Database name is required in the MongoDB connection string (mongodb://.../<database>)."} );
            }

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
        public async Task<IActionResult> GetCollectionsForSavedConnection([FromBody] SavedConnectionRequest request, CancellationToken cancellationToken)
        {
            var dataSource = gisDbService.GetDataSourceById(request.DataSourceId);
            if (dataSource is null)
            {
                return BadRequest(new { message = "Saved connection not found." });
            }

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
        public async Task<IActionResult> GetMongoSampleForSavedConnection([FromBody] SavedConnectionSampleRequest request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.CollectionName))
            {
                return BadRequest(new { message = "Collection is required." });
            }

            var dataSource = gisDbService.GetDataSourceById(request.DataSourceId);
            if (dataSource is null)
            {
                return BadRequest(new { message = "Saved connection not found." });
            }

            try
            {
                var databaseName = GetDatabaseName(dataSource.ConnectionString);
                var client = new MongoClient(dataSource.ConnectionString);
                var database = client.GetDatabase(databaseName);
                var collection = database.GetCollection<BsonDocument>(request.CollectionName.Trim());
                var sample = await collection.Find(FilterDefinition<BsonDocument>.Empty).Limit(1).FirstOrDefaultAsync(cancellationToken);

                if (sample is null)
                {
                    return Ok(new
                    {
                        hasSample = false,
                        fields = Array.Empty<string>(),
                        sampleJson = "{}"
                    });
                }

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
                SavedMongoConnections = gisDbService.GetMongoDataSources()
            };
        }

        private static List<PropertyMapping> ParseMappings(string mappingsText)
        {
            var lines = mappingsText
                .Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            var mappings = new List<PropertyMapping>();
            foreach (var line in lines)
            {
                var parts = line.Split('|', 3, StringSplitOptions.TrimEntries);
                if (parts.Length < 2 || string.IsNullOrWhiteSpace(parts[0]) || string.IsNullOrWhiteSpace(parts[1]))
                {
                    continue;
                }

                var columnType = PropertyType.Normal;
                if (parts.Length == 3 && Enum.TryParse<PropertyType>(parts[2], true, out var parsedType))
                {
                    columnType = parsedType;
                }

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
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                return (false, "MongoDB connection string is required.");
            }

            var resolvedDatabaseName = GetDatabaseName(connectionString);
            if (string.IsNullOrWhiteSpace(resolvedDatabaseName))
            {
                return (false, "Database name is required in the MongoDB connection string (mongodb://.../<database>).");
            }

            try
            {
                var client = new MongoClient(connectionString.Trim());
                var database = client.GetDatabase(resolvedDatabaseName);
                var collections = await database.ListCollectionNames().ToListAsync(cancellationToken);
                if (!string.IsNullOrWhiteSpace(collectionName) &&
                    !collections.Contains(collectionName.Trim(), StringComparer.Ordinal))
                {
                    return (false, $"Collection '{collectionName}' was not found in database '{resolvedDatabaseName}'.");
                }
            }
            catch (Exception ex)
            {
                return (false, $"MongoDB connection failed: {ex.Message}");
            }

            return (true, string.Empty);
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
}
