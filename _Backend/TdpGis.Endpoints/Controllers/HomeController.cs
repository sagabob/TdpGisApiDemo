using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using TdpGis.AdminApplication.AppModels;
using TdpGis.AdminApplication.Services;
using TdpGis.Endpoints.Models;
using TdpGis.Endpoints.Security;

namespace TdpGis.Endpoints.Controllers;

[Authorize(Policy = "GisPortalAccess")]
public class HomeController(IGisAdminAppService gisAdmin, IConfiguration configuration) : Controller
{
    private string AdminAppRole => configuration["AzureAd:AdminAppRole"] ?? "Gis.Admin";
    [AllowAnonymous]
    public IActionResult Index()
    {
        return View("Project");
    }

    public IActionResult Configuration(Guid? gisEdit)
    {
        var data = gisAdmin.GetConfigurationPageData();
        var model = MapToPageViewModel(data);
        ApplyViewerAccess(model);
        if (gisEdit.HasValue && model.CanManageConfiguration)
        {
            var conn = gisAdmin.GetGisConnectionById(gisEdit.Value);
            if (conn is not null)
            {
                var formState = new GisConnectionFormState();
                gisAdmin.MapGisConnectionToForm(formState, conn);
                MapFormStateToViewModel(model.Form, formState);
            }
        }

        return View("Index", model);
    }

    [HttpPost]
    [Authorize(Policy = "GisConfigurationAdmin")]
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

        var result = await gisAdmin.SaveMongoDataSourceAsync(model.ConnectionString, cancellationToken);
        if (!result.IsSuccess)
        {
            ApplyFormResultToModelState(result);
            var pageModel = BuildPageModel();
            pageModel.MongoForm = model;
            return View("Index", pageModel);
        }

        TempData["SuccessMessage"] = result.SuccessMessage;
        return RedirectToAction(nameof(Configuration));
    }

    [HttpPost]
    [Authorize(Policy = "GisConfigurationAdmin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveGisConnection([Bind(Prefix = "Form")] GisConnectionFormViewModel model,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            var pageModel = BuildPageModel();
            pageModel.Form = model;
            return View("Index", pageModel);
        }

        var input = new SaveGisConnectionInput(
            model.GisConnectionId,
            model.DataSourceId,
            model.Name,
            model.Description,
            model.Entity,
            model.EntityLabel,
            model.QueryField,
            model.GeometryType,
            model.GisWorkspaceId,
            model.PropertyMappingsText);

        var result = await gisAdmin.SaveGisConnectionAsync(input, cancellationToken);
        if (!result.IsSuccess)
        {
            ApplyFormResultToModelState(result);
            var pageModel = BuildPageModel();
            pageModel.Form = model;
            return View("Index", pageModel);
        }

        TempData["SuccessMessage"] = result.SuccessMessage;
        if (result.RedirectGisEditId.HasValue)
            return RedirectToAction(nameof(Configuration), new { gisEdit = result.RedirectGisEditId.Value });

        return RedirectToAction(nameof(Configuration));
    }

    [HttpPost]
    [Authorize(Policy = "GisConfigurationAdmin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AssignEntitiesToWorkspace(
        [Bind(Prefix = "AssignEntitiesForm")] AssignEntitiesWorkspaceFormViewModel model,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            var pageModel = BuildPageModel();
            pageModel.AssignEntitiesForm = model;
            return View("Index", pageModel);
        }

        var input = new AssignEntitiesInput(model.WorkspaceId, model.SelectedConnectionIds ?? []);
        var result = await gisAdmin.AssignEntitiesToWorkspaceAsync(input, cancellationToken);
        if (!result.IsSuccess)
        {
            ApplyFormResultToModelState(result);
            var pageModel = BuildPageModel();
            pageModel.AssignEntitiesForm = model;
            return View("Index", pageModel);
        }

        TempData["SuccessMessage"] = result.SuccessMessage;
        return RedirectToAction(nameof(Configuration));
    }

    [HttpPost]
    [Authorize(Policy = "GisConfigurationAdmin")]
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

        var input = new SaveWorkspaceInput(model.WorkspaceId, model.Name);
        var result = await gisAdmin.SaveWorkspaceAsync(input, cancellationToken);
        if (!result.IsSuccess)
        {
            ApplyFormResultToModelState(result);
            var pageModel = BuildPageModel();
            pageModel.WorkspaceForm = model;
            return View("Index", pageModel);
        }

        TempData["SuccessMessage"] = result.SuccessMessage;
        return RedirectToAction(nameof(Configuration));
    }

    [HttpPost]
    [Authorize(Policy = "GisConfigurationAdmin")]
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

        var input = new CreateAccessTokenInput(
            model.WorkspaceId,
            model.Name,
            AccessTokenExpiryEndOfLocalDay(model.ExpiredDateTime),
            model.IsActive,
            model.IsPublic);
        var result = await gisAdmin.CreateWorkspaceAccessTokenAsync(input, cancellationToken);
        if (!result.IsSuccess)
        {
            ApplyFormResultToModelState(result);
            var pageModel = BuildPageModel();
            pageModel.AccessTokenForm = model;
            return View("Index", pageModel);
        }

        TempData["SuccessMessage"] = result.SuccessMessage;
        TempData["CreatedAccessTokenPlain"] = result.CreatedAccessTokenPlain;
        return RedirectToAction(nameof(Configuration));
    }

    [HttpPost]
    [Authorize(Policy = "GisConfigurationAdmin")]
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

        var input = new UpdateAccessTokenInput(
            model.TokenId,
            model.GisWorkspaceId,
            model.Name,
            AccessTokenExpiryEndOfLocalDay(model.ExpiredDateTime),
            model.IsActive,
            model.IsPublic);
        var result = await gisAdmin.UpdateWorkspaceAccessTokenAsync(input, cancellationToken);
        if (!result.IsSuccess)
        {
            ApplyFormResultToModelState(result);
            var pageModel = BuildPageModel();
            pageModel.UpdateTokenForm = model;
            return View("Index", pageModel);
        }

        TempData["SuccessMessage"] = result.SuccessMessage;
        return RedirectToAction(nameof(Configuration));
    }

    [HttpPost]
    [Authorize(Policy = "GisConfigurationAdmin")]
    public async Task<IActionResult> ValidateMongoConnection([FromBody] MongoValidationRequest request,
        CancellationToken cancellationToken)
    {
        var response = await gisAdmin.ValidateMongoConnectionAsync(request.ConnectionString, cancellationToken);
        if (!response.Ok)
            return BadRequest(new { message = response.Message });

        return Ok(new
        {
            databaseName = response.DatabaseName,
            collections = response.Collections
        });
    }

    [HttpPost]
    [Authorize(Policy = "GisConfigurationAdmin")]
    public async Task<IActionResult> GetCollectionsForSavedConnection([FromBody] SavedConnectionRequest request,
        CancellationToken cancellationToken)
    {
        var response = await gisAdmin.GetCollectionsForDataSourceAsync(request.DataSourceId, cancellationToken);
        if (!response.Ok)
            return BadRequest(new { message = response.Message });

        return Ok(new { collections = response.Collections });
    }

    [HttpPost]
    [Authorize(Policy = "GisConfigurationAdmin")]
    public async Task<IActionResult> GetMongoSampleForSavedConnection([FromBody] SavedConnectionSampleRequest request,
        CancellationToken cancellationToken)
    {
        var response =
            await gisAdmin.GetMongoSampleAsync(request.DataSourceId, request.CollectionName, cancellationToken);
        if (!response.Ok)
            return BadRequest(new { message = response.Message });

        return Ok(new
        {
            hasSample = response.HasSample,
            fields = response.Fields,
            sampleJson = response.SampleJson
        });
    }

    [AllowAnonymous]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }

    /// <summary>Shown when a signed-in user has neither Gis.Admin nor Gis.Viewer (cookie access denied redirect).</summary>
    [AllowAnonymous]
    public IActionResult AccessDenied()
    {
        return View();
    }

    private GisConnectionPageViewModel BuildPageModel()
    {
        var model = MapToPageViewModel(gisAdmin.GetConfigurationPageData());
        ApplyViewerAccess(model);
        return model;
    }

    private void ApplyViewerAccess(GisConnectionPageViewModel model)
    {
        model.CanManageConfiguration = EntraAppRoleClaims.HasRole(User, AdminAppRole);
    }

    private static GisConnectionPageViewModel MapToPageViewModel(GisConfigurationPageData data)
    {
        return new GisConnectionPageViewModel
        {
            ExistingConnections = data.ExistingConnections.ToList(),
            SavedMongoConnections = data.SavedMongoConnections.ToList(),
            Workspaces = data.Workspaces.ToList()
        };
    }

    private static void MapFormStateToViewModel(GisConnectionFormViewModel form, GisConnectionFormState state)
    {
        form.GisConnectionId = state.GisConnectionId;
        form.DataSourceId = state.DataSourceId;
        form.Name = state.Name;
        form.Description = state.Description;
        form.Entity = state.Entity;
        form.EntityLabel = state.EntityLabel;
        form.QueryField = state.QueryField;
        form.GeometryType = state.GeometryType;
        form.GisWorkspaceId = state.GisWorkspaceId;
        form.PropertyMappingsText = state.PropertyMappingsText;
    }

    /// <summary>
    ///     Maps a calendar date from the date-only picker to the last instant of that day in local time, so the token
    ///     remains valid for the full selected day.
    /// </summary>
    private static DateTime AccessTokenExpiryEndOfLocalDay(DateTime selectedDate)
    {
        var localDate = selectedDate.Kind switch
        {
            DateTimeKind.Utc => selectedDate.ToLocalTime().Date,
            DateTimeKind.Local => selectedDate.Date,
            DateTimeKind.Unspecified => selectedDate.Date,
            _ => selectedDate.Date
        };
        var startOfDay = DateTime.SpecifyKind(localDate, DateTimeKind.Local);
        return startOfDay.AddDays(1).AddTicks(-1);
    }

    private void ApplyFormResultToModelState(FormActionResult result)
    {
        foreach (var kv in result.FieldErrors)
            ModelState.AddModelError(kv.Key, kv.Value);
        if (!string.IsNullOrEmpty(result.ModelOnlyError))
            ModelState.AddModelError(string.Empty, result.ModelOnlyError);
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