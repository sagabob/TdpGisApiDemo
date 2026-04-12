using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace TdpGis.Api.Tests.Support;

/// <summary>
///     Runs <see cref="IntegrationTestAuth.BypassJwtValidation" /> in <see cref="ConfigureTestServices" />
///     so JWT options are adjusted after Microsoft.Identity.Web registers the API.
/// </summary>
internal sealed class TdpGisApiWebApplicationFactory(Action<IServiceCollection> configureTestServices)
    : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, cfg) => IntegrationTestAuth.AddTestAzureAd(cfg));
        builder.ConfigureTestServices(services =>
        {
            IntegrationTestAuth.BypassJwtValidation(services);
            configureTestServices(services);
        });
    }
}