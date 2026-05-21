using TdpGis.LocalApi.DatabaseServices;
using TdpGis.LocalApi.Models;

namespace TdpGis.LocalApi;

public static class Extension
{
    public static void RegisterComponents(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<SearchFeature>(
            configuration.GetSection("SearchFeature")).AddOptionsWithValidateOnStart<Address>();
        services.AddScoped<IGisMongoQueryRepository, GisMongoQueryRepository>();
    }
}