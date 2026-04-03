using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TdpGis.Application.DatabaseService;
using TdpGis.ApplicationDb.Database;
using TdpGis.ApplicationDb.DatabaseService;

namespace TdpGis.ApplicationDb;

public static class Extensions
{
    public static IServiceCollection AddAppDatabaseConfiguration(this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<GisAppDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("Database")));

        services.AddScoped<IGisDbService, GisDbService>();

        return services;
    }
}