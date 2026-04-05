using FastEndpoints;
using FastEndpoints.Swagger;
using Microsoft.AspNetCore.HttpOverrides;
using TdpGis.Infrastructure.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    // Trust forwarded headers from platform load balancers (e.g. DigitalOcean App Platform).
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
builder.Services.AddFastEndpoints()
    .SwaggerDocument(o =>
    {
        o.DocumentSettings = s =>
        {
            s.Title = "Tdp Gis API";
            s.Version = "v1";
        };
    });

var app = builder.Build();

app.UseForwardedHeaders();

// In Docker / behind a reverse proxy, Kestrel is HTTP-only; TLS is terminated upstream.
// HttpsRedirection then has no local HTTPS port and logs a warning — skip it in Production.
if (app.Environment.IsDevelopment())
    app.UseHttpsRedirection();

app.UseCors();

app.UseDefaultExceptionHandler()
    .UseFastEndpoints()
    .UseSwaggerGen();

app.Run();