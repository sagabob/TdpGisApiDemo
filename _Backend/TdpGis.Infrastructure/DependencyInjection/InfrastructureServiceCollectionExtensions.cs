using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using TdpGis.AdminApplication.Abstractions;
using TdpGis.Application.Abstractions;
using TdpGis.Infrastructure.Mongo;
using TdpGis.Infrastructure.Persistence;
using TdpGis.Infrastructure.Sql;

namespace TdpGis.Infrastructure.DependencyInjection;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var appDbConnectionString = CapNpgsqlPool(
            configuration.GetConnectionString("Database"),
            SqlProbeConnectionLimits.AppDbMaxPoolSize);

        services.AddDbContext<GisAppDbContext>(options =>
            options.UseNpgsql(appDbConnectionString));

        services.AddScoped<IGisConfigurationRepository, GisConfigurationRepository>();
        services.AddScoped<IGisConfigurationService, GisConfigurationService>();

        services.AddSingleton<MongoClientCache>();
        services.AddSingleton<PostgresDataSourceCache>();

        services.AddScoped<IGisMongoQueryRepository, GisMongoQueryRepository>();
        services.AddScoped<IMongoMetadataProvider, MongoMetadataProvider>();
        services.AddScoped<ISqlMetadataProvider, SqlMetadataProvider>();
        services.AddScoped<IGisSqlQueryRepository, GisSqlQueryRepository>();
        services.AddScoped<GisMongoDataService>();
        services.AddScoped<GisSqlDataService>();
        services.AddScoped<IGisDataService, GisDataService>();

        return services;
    }

    private static string? CapNpgsqlPool(string? connectionString, int maxPoolSize)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            return connectionString;

        var builder = new NpgsqlConnectionStringBuilder(connectionString)
        {
            MaxPoolSize = maxPoolSize
        };
        return builder.ConnectionString;
    }
}