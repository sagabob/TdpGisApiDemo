// =============================================================================
// TdpGis.Api — composition root
//
// Auth: Entra JWT Bearer + app role (TdpGisApi.Access) on FastEndpoints; workspace
//       token via X-Access-Token (validated in Application). See AuthenticationServiceExtensions.
// =============================================================================

using TdpGis.Api.Authentication;
using TdpGis.Api.Health;
using TdpGis.Api.Hosting;
using TdpGis.Application.DependencyInjection;
using TdpGis.Infrastructure.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

builder.AddTdpGisApiTelemetry();
builder.Services.AddTdpGisApiForwardedHeaders();
builder.Services.AddTdpGisApiCors();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApplication();
builder.Services.AddTdpGisApiHealthChecks();
builder.Services.AddTdpGisApiAuthentication(builder.Configuration);
builder.Services.AddTdpGisApiAuthorization(builder.Configuration);
builder.Services.AddTdpGisApiFastEndpoints();

var app = builder.Build();
app.UseTdpGisApiPipeline();
app.Run();