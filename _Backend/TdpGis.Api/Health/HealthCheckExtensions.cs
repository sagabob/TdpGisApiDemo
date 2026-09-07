using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using TdpGis.Infrastructure.Persistence;

namespace TdpGis.Api.Health;

public static class HealthCheckExtensions
{
    /// <summary>
    ///     Liveness: process is up. Readiness: can reach PostgreSQL (same pattern as TdpGis.Endpoints).
    /// </summary>
    public static IServiceCollection AddTdpGisApiHealthChecks(this IServiceCollection services)
    {
        services.AddHealthChecks()
            .AddCheck("self", () => HealthCheckResult.Healthy(), ["live"])
            .AddDbContextCheck<GisAppDbContext>("database", tags: ["ready"]);

        return services;
    }

    /// <summary>
    ///     Anonymous so probes work without Entra JWT (orchestrators, load balancers, Docker healthcheck).
    /// </summary>
    public static WebApplication MapTdpGisApiHealthChecks(this WebApplication app)
    {
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

        return app;
    }
}