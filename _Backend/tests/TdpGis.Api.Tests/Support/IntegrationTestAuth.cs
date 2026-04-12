using Microsoft.Extensions.Configuration;

namespace TdpGis.Api.Tests.Support;

/// <summary>
///     Test JWT and in-memory config for <see cref="TdpGisApiWebApplicationFactory" />.
///     Supplies fake <c>AzureAd</c> values. <c>IntegrationTests:UseMockJwt</c> and
///     <c>IntegrationTests:SkipApiAccessRole</c> are set via <c>IWebHostBuilder.UseSetting</c> in
///     <see cref="TdpGisApiWebApplicationFactory" /> so minimal hosting sees them before
///     <c>WebApplicationBuilder.Configuration</c> is built.
/// </summary>
internal static class IntegrationTestAuth
{
    public const string TestBearerToken =
        "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwiaWF0IjoxNTE2MjM5MDIyfQ.SflKxwRJSMeKKF2QT4fwpMeJf36POk6yJV_adQssw5c";

    public static void AddTestAzureAd(IConfigurationBuilder cfg)
    {
        cfg.AddInMemoryCollection(
            new Dictionary<string, string?>
            {
                ["AzureAd:Instance"] = "https://login.microsoftonline.com/",
                ["AzureAd:TenantId"] = "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
                ["AzureAd:ClientId"] = "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb",
                ["AzureAd:Audience"] = "api://bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb",
                ["AzureAd:ApiAccessAppRole"] = "TdpGisApi.Access"
            });
    }
}