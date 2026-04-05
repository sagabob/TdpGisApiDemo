using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TdpGis.AdminApplication.Abstractions;
using TdpGis.Application.Abstractions;
using TdpGis.Infrastructure.Mongo;
using TdpGis.Infrastructure.Persistence;

namespace TdpGis.Infrastructure.DependencyInjection;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<GisAppDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("Database")));

        services.AddScoped<IGisConfigurationRepository, GisConfigurationRepository>();

        services.AddScoped<IGisConfigurationService, GisConfigurationService>();

        services.AddSingleton<MongoClientCache>();

        services.AddScoped<IMongoMetadataProvider, MongoMetadataProvider>();

        services.AddScoped<IGisDataService, GisMongoDataService>();

        return services;
    }
}