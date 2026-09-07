using FastEndpoints;
using FastEndpoints.Swagger;
using NSwag;
using TdpGis.Api.Authentication;
using TdpGis.Api.GisQuery.Helpers;
using TdpGis.Api.Health;

namespace TdpGis.Api.Hosting;

public static class FastEndpointsExtensions
{
    /// <summary>
    ///     Registers FastEndpoints and Swagger with Entra Bearer + workspace access token schemes.
    ///     EnableJWTBearerAuth = false avoids a duplicate generic JWT entry alongside the explicit "Entra" scheme.
    /// </summary>
    public static IServiceCollection AddTdpGisApiFastEndpoints(this IServiceCollection services)
    {
        services.AddFastEndpoints()
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
                        Description =
                            "Microsoft Entra ID access token (Authorization: Bearer). Caller must have app role TdpGisApi.Access (or AzureAd:ApiAccessAppRole) in the `roles` claim."
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

        return services;
    }

    /// <summary>
    ///     HTTP pipeline: forwarded headers → CORS → auth → health → FastEndpoints (with API access policy) → Swagger.
    ///     Policy is applied to FastEndpoints only (not FallbackPolicy), so Swagger stays anonymous.
    /// </summary>
    public static WebApplication UseTdpGisApiPipeline(this WebApplication app)
    {
        app.UseForwardedHeaders();

        if (app.Environment.IsDevelopment())
            app.UseHttpsRedirection();

        app.UseCors();

        // Order matters: authenticate JWT first, then run authorization, then endpoints.
        app.UseAuthentication();
        app.UseAuthorization();

        app.MapTdpGisApiHealthChecks();

        app.UseDefaultExceptionHandler()
            .UseFastEndpoints(c =>
                c.Endpoints.Configurator = ep => ep.Policies(TdpGisApiAuthPolicies.Access))
            .UseSwaggerGen();

        return app;
    }
}