namespace TdpGis.Api.Hosting;

public static class CorsExtensions
{
    public static IServiceCollection AddTdpGisApiCors(this IServiceCollection services)
    {
        services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
                policy.AllowAnyOrigin()
                    .AllowAnyHeader()
                    .AllowAnyMethod());
        });

        return services;
    }
}