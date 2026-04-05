using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection;
using TdpGis.Infrastructure.Persistence;

namespace TdpGis.Infrastructure.DependencyInjection;

public static class EndpointsDataProtectionExtensions
{
    /// <summary>
    ///     Stores ASP.NET Data Protection keys in PostgreSQL so cookie auth survives Docker restarts without a file volume.
    ///     Call after <see cref="InfrastructureServiceCollectionExtensions.AddInfrastructure" />.
    /// </summary>
    public static void AddTdpGisEndpointsDataProtection(this IServiceCollection services)
    {
        services.AddDataProtection()
            .PersistKeysToDbContext<GisAppDbContext>()
            .SetApplicationName("TdpGis.Endpoints");
    }
}