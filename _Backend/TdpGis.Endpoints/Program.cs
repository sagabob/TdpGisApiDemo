using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using TdpGis.AdminApplication.DependencyInjection;
using TdpGis.Endpoints.Options;
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

builder.Services.Configure<AdminDashboardOptions>(builder.Configuration.GetSection(AdminDashboardOptions.SectionName));

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddTdpGisEndpointsDataProtection();

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/Login";
        options.SlidingExpiration = true;
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.Cookie.SameSite = SameSiteMode.Lax;
    });

builder.Services.AddAuthorization();

builder.Services.AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy(), tags: ["live"])
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
{
    app.UseWhen(
        ctx => !ctx.Request.Path.StartsWithSegments("/health"),
        branch => branch.UseHttpsRedirection());
}

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