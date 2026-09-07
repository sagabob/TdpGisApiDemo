using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Identity.Web;
using TdpGis.Application.Security;

namespace TdpGis.Api.Authentication;

/// <summary>
///     Auth model (two layers):
///     1) Microsoft Entra ID — JWT in Authorization: Bearer (AddMicrosoftIdentityWebApi + AzureAd).
///     API routes require AzureAd:ApiAccessAppRole (default TdpGisApi.Access) on the roles claim.
///     2) Workspace access — opaque token in X-Access-Token only (resolved in GisWorkspaceAccess,
///     validated in Application WorkspaceAccessGuard). Bearer is never used for workspace tokens.
///     IntegrationTests:UseMockJwt — mock Bearer handler (no Entra validation).
///     IntegrationTests:SkipApiAccessRole — skips the app-role assertion (tests only).
/// </summary>
public static class TdpGisApiAuthPolicies
{
    public const string Access = "TdpGisApiAccess";
}

public static class AuthenticationServiceExtensions
{
    public static IServiceCollection AddTdpGisApiAuthentication(this IServiceCollection services,
        IConfiguration configuration)
    {
        var useMockJwtForIntegrationTests = configuration.GetValue("IntegrationTests:UseMockJwt", false);
        if (useMockJwtForIntegrationTests)
        {
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddScheme<AuthenticationSchemeOptions, IntegrationTestJwtAuthenticationHandler>(
                    JwtBearerDefaults.AuthenticationScheme, _ => { });
            return services;
        }

        // AddMicrosoftIdentityWebApi wires JWT bearer validation to the "AzureAd" config section.
        // Incoming "Authorization: Bearer <token>" is validated (issuer, audience, signature, lifetime).
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddMicrosoftIdentityWebApi(configuration.GetSection("AzureAd"));

        // After Microsoft.Identity.Web: roles claim + accept common Entra audience string forms (api://… vs raw app id).
        // If you still see "audience '(null)' is invalid", the JWT likely has no `aud` claim — use an access token for
        // this API (correct scope), not an ID token or Graph token; decode at jwt.ms and confirm `aud` exists.
        var apiAudience = configuration["AzureAd:Audience"];
        var apiClientId = configuration["AzureAd:ClientId"];
        var acceptedAudiences = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (!string.IsNullOrWhiteSpace(apiAudience)) acceptedAudiences.Add(apiAudience.Trim());
        if (!string.IsNullOrWhiteSpace(apiClientId))
        {
            acceptedAudiences.Add(apiClientId.Trim());
            acceptedAudiences.Add($"api://{apiClientId.Trim()}");
        }

        services.PostConfigure<JwtBearerOptions>(
            JwtBearerDefaults.AuthenticationScheme,
            jwtOptions =>
            {
                jwtOptions.TokenValidationParameters.RoleClaimType = "roles";
                if (acceptedAudiences.Count > 0)
                    jwtOptions.TokenValidationParameters.ValidAudiences = acceptedAudiences.ToArray();
            });

        // Keep claim types as Entra issues them (e.g. "roles") instead of mapped URIs.
        services.Configure<MicrosoftIdentityOptions>(options => { options.MapInboundClaims = false; });

        return services;
    }

    public static IServiceCollection AddTdpGisApiAuthorization(this IServiceCollection services,
        IConfiguration configuration)
    {
        var apiAccessAppRole = configuration["AzureAd:ApiAccessAppRole"] ?? "TdpGisApi.Access";
        var skipApiAccessRoleCheck = configuration.GetValue("IntegrationTests:SkipApiAccessRole", false);

        services.AddAuthorizationBuilder()
            .AddPolicy(TdpGisApiAuthPolicies.Access, policy =>
            {
                policy.AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme);
                policy.RequireAuthenticatedUser();
                if (!skipApiAccessRoleCheck)
                    policy.RequireAssertion(ctx => EntraAppRoleClaims.HasRole(ctx.User, apiAccessAppRole));
            });

        return services;
    }
}