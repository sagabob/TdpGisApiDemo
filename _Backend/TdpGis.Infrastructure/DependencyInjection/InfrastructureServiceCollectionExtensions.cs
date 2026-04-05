using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TdpGis.Application.Abstractions;
using TdpGis.Infrastructure.Mongo;
using TdpGis.Infrastructure.Persistence;

namespace TdpGis.Infrastructure.DependencyInjection;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<GisAppDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("Database")));

        services.AddScoped<IGisConfigurationRepository, GisConfigurationRepository>();
        services.AddScoped<IMongoMetadataProvider, MongoMetadataProvider>();

        return services;
    }
}
