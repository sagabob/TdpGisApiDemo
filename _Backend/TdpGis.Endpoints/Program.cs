using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Identity.Web;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using TdpGis.AdminApplication.DependencyInjection;
using TdpGis.Endpoints.Security;
using TdpGis.Infrastructure.DependencyInjection;
using TdpGis.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

// Add services to the container.
builder.Services.AddControllersWithViews();

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddTdpGisEndpointsDataProtection();

builder.Services
    .AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApp(builder.Configuration.GetSection("AzureAd"));

builder.Services.Configure<CookieAuthenticationOptions>(CookieAuthenticationDefaults.AuthenticationScheme,
    options => { options.AccessDeniedPath = "/Home/AccessDenied"; });

// Ensure 403 from [Authorize] uses the cookie handler so AccessDeniedPath is honored (not OIDC forbid).
builder.Services.Configure<AuthenticationOptions>(options =>
{
    options.DefaultForbidScheme = CookieAuthenticationDefaults.AuthenticationScheme;
});

// Use authorization code flow only (not implicit id_token). Avoids AADSTS700054 unless you enable
// "ID tokens" under Implicit grant in the Entra app registration.
// MapInboundClaims = false keeps claim types such as "roles" as issued (needed for app roles).
// RoleClaimType = "roles" enables User.IsInRole(...) for Entra app roles (AzureAd:AdminAppRole / ViewerAppRole).
builder.Services.Configure<OpenIdConnectOptions>(OpenIdConnectDefaults.AuthenticationScheme, options =>
{
    options.ResponseType = OpenIdConnectResponseType.Code;
    options.SaveTokens = true;
    options.MapInboundClaims = false;
    options.TokenValidationParameters.RoleClaimType = "roles";
});

var adminAppRole = builder.Configuration["AzureAd:AdminAppRole"] ?? "Gis.Admin";
var viewerAppRole = builder.Configuration["AzureAd:ViewerAppRole"] ?? "Gis.Viewer";
builder.Services.AddAuthorization(options =>
{
    // Do not use RequireRole alone: Entra app roles use the "roles" claim; RoleClaimType on the identity may not match.
    options.AddPolicy("GisPortalAccess", policy =>
        policy.RequireAssertion(ctx =>
            EntraAppRoleClaims.HasRole(ctx.User, adminAppRole) ||
            EntraAppRoleClaims.HasRole(ctx.User, viewerAppRole)));
    options.AddPolicy("GisConfigurationAdmin", policy =>
        policy.RequireAssertion(ctx => EntraAppRoleClaims.HasRole(ctx.User, adminAppRole)));
});

builder.Services.AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy(), ["live"])
    .AddDbContextCheck<GisAppDbContext>("database", tags: ["ready"]);

builder.Services.AddAdminApplication();

var app = builder.Build();

app.UseForwardedHeaders();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

// In Docker / behind a reverse proxy, Kestrel is HTTP-only; TLS is at the edge. Skip redirect in Production to avoid
// "Failed to determine the https port" and rely on the platform URL being HTTPS.
// In Development, skip HTTPS redirect for /health* so http://localhost:.../health works without trusting the dev cert.
if (app.Environment.IsDevelopment())
    app.UseWhen(
        ctx => !ctx.Request.Path.StartsWithSegments("/health"),
        branch => branch.UseHttpsRedirection());

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// Register before static assets / MVC so /health is not shadowed; AllowAnonymous so cookie auth never blocks probes.
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

app.MapStaticAssets();

app.MapControllerRoute(
        "default",
        "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();