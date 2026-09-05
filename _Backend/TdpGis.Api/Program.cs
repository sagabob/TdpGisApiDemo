// =============================================================================
// TdpGis.Api — HTTP pipeline & security
//
// Auth model (two layers):
//   1) Microsoft Entra ID — JWT in "Authorization: Bearer". Validated here via
//      AddMicrosoftIdentityWebApi + AzureAd in appsettings. Every API route requires
//      the Entra app role in AzureAd:ApiAccessAppRole (default TdpGisApi.Access) on the
//      token's "roles" claim, plus a named auth policy applied to FastEndpoints only (not FallbackPolicy,
//      which would block NSwag /swagger and OpenAPI JSON).
//   2) Workspace access — opaque token in "X-Access-Token" only. Resolved in GisWorkspaceAccess,
//      validated in Application use cases (WorkspaceAccessGuard) against the database.
//      Bearer is reserved for Entra, never for workspace.
//
// Config: AzureAd section (TenantId, ClientId, Audience, etc.) — see appsettings.
// IntegrationTests:UseMockJwt — when true, Bearer is handled by IntegrationTestJwtAuthenticationHandler
// (no Entra validation). Tests set this via IWebHostBuilder.UseSetting so minimal hosting picks it up.
// =============================================================================

using FastEndpoints;
using FastEndpoints.Swagger;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Identity.Web;
using NSwag;
using TdpGis.Api.Authentication;
using TdpGis.Api.GisQuery.Helpers;
using TdpGis.Infrastructure.DependencyInjection;
using TdpGis.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

// Reverse proxy: trust X-Forwarded-* when hosted behind a load balancer.
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod());
});

builder.Services.AddOpenApi();
builder.Services.AddInfrastructure(builder.Configuration);

// Liveness: process is up. Readiness: can reach PostgreSQL (same pattern as TdpGis.Endpoints).
builder.Services.AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy(), ["live"])
    .AddDbContextCheck<GisAppDbContext>("database", tags: ["ready"]);

// --- Authentication: Entra access tokens as Bearer JWT (or mock JWT for integration tests) ------------
var useMockJwtForIntegrationTests = builder.Configuration.GetValue("IntegrationTests:UseMockJwt", false);
if (useMockJwtForIntegrationTests)
{
    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddScheme<AuthenticationSchemeOptions, IntegrationTestJwtAuthenticationHandler>(
            JwtBearerDefaults.AuthenticationScheme, _ => { });
}
else
{
    // AddMicrosoftIdentityWebApi wires JWT bearer validation to the "AzureAd" config section.
    // Incoming "Authorization: Bearer <token>" is validated (issuer, audience, signature, lifetime).
    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAd"));

    // After Microsoft.Identity.Web: roles claim + accept common Entra audience string forms (api://… vs raw app id).
    // If you still see "audience '(null)' is invalid", the JWT likely has no `aud` claim — use an access token for
    // this API (correct scope), not an ID token or Graph token; decode at jwt.ms and confirm `aud` exists.
    var apiAudience = builder.Configuration["AzureAd:Audience"];
    var apiClientId = builder.Configuration["AzureAd:ClientId"];
    var acceptedAudiences = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    if (!string.IsNullOrWhiteSpace(apiAudience)) acceptedAudiences.Add(apiAudience.Trim());
    if (!string.IsNullOrWhiteSpace(apiClientId))
    {
        acceptedAudiences.Add(apiClientId.Trim());
        acceptedAudiences.Add($"api://{apiClientId.Trim()}");
    }

    builder.Services.PostConfigure<JwtBearerOptions>(
        JwtBearerDefaults.AuthenticationScheme,
        jwtOptions =>
        {
            jwtOptions.TokenValidationParameters.RoleClaimType = "roles";
            if (acceptedAudiences.Count > 0)
                jwtOptions.TokenValidationParameters.ValidAudiences = acceptedAudiences.ToArray();
        });

    // Keep claim types as Entra issues them (e.g. "roles") instead of mapped URIs.
    builder.Services.Configure<MicrosoftIdentityOptions>(options => { options.MapInboundClaims = false; });
}

// --- Authorization: Entra JWT + app role (Expose an API → App roles → assign users/groups) ---
// Token must include app role value AzureAd:ApiAccessAppRole (default TdpGisApi.Access) on a "roles" claim.
// IntegrationTests:SkipApiAccessRole=true (injected only by TdpGis.Api.Tests) skips the role assertion.
var apiAccessAppRole = builder.Configuration["AzureAd:ApiAccessAppRole"] ?? "TdpGisApi.Access";
var skipApiAccessRoleCheck = builder.Configuration.GetValue("IntegrationTests:SkipApiAccessRole", false);
const string tdpGisApiAccessPolicy = "TdpGisApiAccess";
builder.Services.AddAuthorizationBuilder()
    .AddPolicy(tdpGisApiAccessPolicy, policy =>
        {
            policy.AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme);
            policy.RequireAuthenticatedUser();
            if (!skipApiAccessRoleCheck)
                policy.RequireAssertion(ctx => EntraAppRoleClaims.HasRole(ctx.User, apiAccessAppRole));
        });

// Swagger: document both schemes — Entra (Bearer) for API auth, X-Access-Token for workspace GIS calls.
// EnableJWTBearerAuth = false avoids duplicate generic JWT entries; we register "Entra" explicitly below.
builder.Services.AddFastEndpoints()
    .SwaggerDocument(o =>
    {
        o.EnableJWTBearerAuth = false;
        o.DocumentSettings = s =>
        {
            s.Title = "Tdp Gis API";
            s.Version = "v1";
            s.AddAuth("Entra", new OpenApiSecurityScheme
            {
                Type = OpenApiSecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                Description =
                    "Microsoft Entra ID access token (Authorization: Bearer). Caller must have app role TdpGisApi.Access (or AzureAd:ApiAccessAppRole) in the `roles` claim."
            });
            s.AddAuth("WorkspaceAccess", new OpenApiSecurityScheme
            {
                Type = OpenApiSecuritySchemeType.ApiKey,
                In = OpenApiSecurityApiKeyLocation.Header,
                Name = GisWorkspaceAccess.AccessTokenHeader,
                Description = "Workspace access token (GIS query routes)."
            });
        };
    });

var app = builder.Build();

app.UseForwardedHeaders();

if (app.Environment.IsDevelopment())
    app.UseHttpsRedirection();

app.UseCors();

// Order matters: authenticate JWT first, then run authorization, then endpoints.
app.UseAuthentication();
app.UseAuthorization();

// Anonymous so probes work without Entra JWT (orchestrators, load balancers, Docker healthcheck).
app.MapHealthChecks("/health", new HealthCheckOptions
    {
        Predicate = r => r.Tags?.Contains("live") == true
    })
    .AllowAnonymous();
app.MapHealthChecks("/health/ready", new HealthCheckOptions
    {
        Predicate = r => r.Tags?.Contains("ready") == true
    })
    .AllowAnonymous();

app.UseDefaultExceptionHandler()
    .UseFastEndpoints(c => c.Endpoints.Configurator = ep => ep.Policies(tdpGisApiAccessPolicy))
    .UseSwaggerGen();

app.Run();