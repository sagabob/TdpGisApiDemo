using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace TdpGis.Api.Tests.Support;

/// <summary>
///     Injects test AzureAd config (<see cref="IntegrationTestAuth.AddTestAzureAd" />) so the API runs with
///     mock JWT auth and skips the Entra app role requirement.
/// </summary>
internal sealed class TdpGisApiWebApplicationFactory(Action<IServiceCollection> configureTestServices)
    : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Host settings must be visible when Program builds WebApplicationBuilder.Configuration (minimal hosting).
        builder.UseSetting("IntegrationTests:UseMockJwt", "true");
        builder.UseSetting("IntegrationTests:SkipApiAccessRole", "true");
        builder.ConfigureAppConfiguration((_, cfg) => IntegrationTestAuth.AddTestAzureAd(cfg));
        builder.ConfigureTestServices(configureTestServices);
    }
}