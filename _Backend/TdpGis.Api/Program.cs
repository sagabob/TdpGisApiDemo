// =============================================================================
// TdpGis.Api — HTTP pipeline & security
//
// Auth model (two layers):
//   1) Microsoft Entra ID — JWT in "Authorization: Bearer". Validated here via
//      AddMicrosoftIdentityWebApi + AzureAd in appsettings. Required for every
//      API route (FallbackPolicy + FastEndpoints secure by default).
//   2) Workspace access — opaque token in "X-Access-Token" only. Not validated
//      in this file; GIS endpoints use GisWorkspaceAccess.TryValidateAsync
//      against the database. Bearer is reserved for Entra, never for workspace.
//
// Config: AzureAd section (TenantId, ClientId, Audience, etc.) — see appsettings.
// =============================================================================

using FastEndpoints;
using FastEndpoints.Swagger;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Identity.Web;
using NSwag;
using TdpGis.Api.GisQuery.Helpers;
using TdpGis.Infrastructure.DependencyInjection;

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

// --- Authentication: Entra access tokens as Bearer JWT ---------------------------------
// AddMicrosoftIdentityWebApi wires JWT bearer validation to the "AzureAd" config section.
// Incoming "Authorization: Bearer <token>" is validated (issuer, audience, signature, lifetime).
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAd"));

// Entra app roles arrive in the "roles" claim; align with ClaimsPrincipal role checks.
builder.Services.PostConfigure<JwtBearerOptions>(
    JwtBearerDefaults.AuthenticationScheme,
    jwtOptions => { jwtOptions.TokenValidationParameters.RoleClaimType = "roles"; });

// Keep claim types as Entra issues them (e.g. "roles") instead of mapped URIs.
builder.Services.Configure<MicrosoftIdentityOptions>(options => { options.MapInboundClaims = false; });

// --- Authorization: require a signed-in user on all endpoints by default --------------
// FallbackPolicy applies when an endpoint does not call AllowAnonymous().
// Together with FastEndpoints' secure-by-default behavior, every route needs a valid Entra JWT.
builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme)
        .RequireAuthenticatedUser()
        .Build();
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
                Description = "Microsoft Entra ID access token for this API (Authorization: Bearer)."
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

// Order matters: authenticate JWT first, then run authorization (FallbackPolicy), then endpoints.
app.UseAuthentication();
app.UseAuthorization();

app.UseDefaultExceptionHandler()
    .UseFastEndpoints()
    .UseSwaggerGen();

app.Run();

public partial class Program;