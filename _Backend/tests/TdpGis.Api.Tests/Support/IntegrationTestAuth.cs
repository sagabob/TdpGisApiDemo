using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace TdpGis.Api.Tests.Support;

/// <summary>
///     Bypasses JWT validation in integration tests (no real Entra).
///     Uses
///     <see>
///         <cref>PostConfigureAll{TOptions}</cref>
///     </see>
///     so options run after Microsoft.Identity.Web's own configuration.
/// </summary>
internal static class IntegrationTestAuth
{
    public const string TestBearerToken =
        "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwiaWF0IjoxNTE2MjM5MDIyfQ.SflKxwRJSMeKKF2QT4fwpMeJf36POk6yJV_adQssw5c";

    public static void BypassJwtValidation(IServiceCollection services)
    {
        services.PostConfigureAll<JwtBearerOptions>(options =>
        {
            var p = options.TokenValidationParameters;
            p.ValidateIssuer = false;
            p.ValidateAudience = false;
            p.ValidateLifetime = false;
            p.ValidateIssuerSigningKey = false;
            p.RequireSignedTokens = false;
            p.SignatureValidator = (token, _) =>
            {
                var handler = new JwtSecurityTokenHandler();
                return handler.ReadJwtToken(token);
            };
        });
    }

    public static void AddTestAzureAd(IConfigurationBuilder cfg)
    {
        cfg.AddInMemoryCollection(
            new Dictionary<string, string?>
            {
                ["AzureAd:Instance"] = "https://login.microsoftonline.com/",
                ["AzureAd:TenantId"] = "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
                ["AzureAd:ClientId"] = "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb",
                ["AzureAd:Audience"] = "api://bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"
            });
    }
}