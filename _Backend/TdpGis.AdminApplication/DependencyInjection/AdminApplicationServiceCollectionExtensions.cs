using Microsoft.Extensions.DependencyInjection;
using TdpGis.AdminApplication.Services;

namespace TdpGis.AdminApplication.DependencyInjection;

public static class AdminApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddAdminApplication(this IServiceCollection services)
    {
        services.AddScoped<IGisAdminAppService, GisAdminAppService>();
        return services;
    }
}