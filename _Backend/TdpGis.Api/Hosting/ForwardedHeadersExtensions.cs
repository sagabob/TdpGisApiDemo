using Microsoft.AspNetCore.HttpOverrides;

namespace TdpGis.Api.Hosting;

public static class ForwardedHeadersExtensions
{
    /// <summary>
    ///     Trust X-Forwarded-* when hosted behind a load balancer / Container Apps ingress.
    /// </summary>
    public static IServiceCollection AddTdpGisApiForwardedHeaders(this IServiceCollection services)
    {
        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            options.KnownIPNetworks.Clear();
            options.KnownProxies.Clear();
        });

        return services;
    }
}