using Microsoft.Extensions.DependencyInjection;
using TdpGis.Application.Services;

namespace TdpGis.Application.DependencyInjection;

public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IGisAdminAppService, GisAdminAppService>();
        return services;
    }
}
